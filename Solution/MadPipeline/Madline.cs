using System;
using System.Buffers;
using System.Diagnostics;

namespace MadPipeline
{
    public sealed partial class Madline
    {
        // 4KB(DefaultMinimumSegmentSize == 4096)을 곱할 시 용량상 64(혹은 65?)KB
        public const int InitialSegmentPoolSize = 16;
        // 4KB(DefaultMinimumSegmentSize)을 곱할 시 용량으로는 1MB, 값일때 스택 크기를 넘기지 못하도록 한 것 같음
        public const int MaxSegmentPoolSize = 256;

        private int targetBytes;

        // 마지막 검사한 지점, 얼마나 많은 바이트가 해제되어야하는지 계산하기 위해
        private long lastExaminedIndex = -1;
        // flush됐지만 reader에 의해 사용되지 않은 바이트
        private long unconsumedBytes;
        // written했지만 flush되지 않은 바이트
        private long notFlushedBytes;

        private MadlineOperationState operationState;
        
        private BufferSegment? readHead;
        private int readHeadIndex;

        private readonly int maxPooledBufferSize;
        private bool disposed;

        private BufferSegment? readTail;
        private int readTailIndex;

        private BufferSegment? writingHead;
        private Memory<byte> writingHeadMemory;
        private int writingHeadBytesBuffered;

        private readonly MemoryPool<byte>? pool;
        // Pool에서 요청한 세그먼트의 최소 크기
        private readonly int minimumSegmentSize;
        // Flush()에서 차단을 시작할 때 Pipe의 바이트 수
        private readonly long pauseWriterThreshold;
        // Flush()가 차단을 중지할 때 Pipe의 바이트 수
        private readonly long resumeWriterThreshold;

        // 버퍼 메모리풀 관리 기존 파이프라인대로 운용
        private BufferSegmentStack bufferSegmentPool;

        private bool isWriterCompleted;
        private bool isReaderCompleted;

        public Madline(MadlineOptions options)
        {
            bufferSegmentPool = new BufferSegmentStack(InitialSegmentPoolSize);

            this.operationState = default;
            
            this.pool = options.Pool == MemoryPool<byte>.Shared ? null : options.Pool;
            this.maxPooledBufferSize = this.pool?.MaxBufferSize ?? 0;
            this.minimumSegmentSize = options.MinimumSegmentSize;
            this.pauseWriterThreshold = options.PauseWriterThreshold;
            this.resumeWriterThreshold = options.ResumeWriterThreshold;
            this.targetBytes = options.TargetBytes;
            this.Writer = new MadlineWriter(this);
            this.Reader = new MadlineReader(this);
            this.Callback = new MadlineSignals();
        }

        public long Length => this.unconsumedBytes;

        public MadlineReader Reader { get; }
        public MadlineWriter Writer { get; }
        public MadlineSignals Callback { get; }

        // 당장은 안 쓰고 있으나, 재사용을 위해 삭제하지는 않았음
        public void ResetState()
        {
            this.ClearReservedRead();
            this.ClearReservedWrite();
            this.operationState = default;
            this.isWriterCompleted = false;
            this.isReaderCompleted = false;
            this.readTailIndex = 0;
            this.readHeadIndex = 0;
            this.lastExaminedIndex = -1;
            this.notFlushedBytes = 0;
            this.unconsumedBytes = 0;
        }


        // 할당
        public Memory<byte> GetMemory(int sizeHint)
        {
            if (this.isWriterCompleted)
            {
                throw new Exception();
            }
            if (sizeHint < 0)
            {
                sizeHint = 0;
            }
            this.AllocateWriteHeadIfNeeded(sizeHint);
            return this.writingHeadMemory;
        }

        public Span<byte> GetSpan(int sizeHint)
        {
            this.AllocateWriteHeadIfNeeded(sizeHint);

            return this.writingHeadMemory.Span;
        }
        
        private void AllocateWriteHeadIfNeeded(int sizeHint)
        {
            if (!this.operationState.IsWritingActive ||
                this.writingHeadMemory.Length == 0 || this.writingHeadMemory.Length < sizeHint)
            {
                this.AllocateWriteHeadSynchronized(sizeHint);
            }
        }
        private void AllocateWriteHeadSynchronized(int sizeHint)
        {
            this.operationState.BeginWrite();

            // 이럴 경우는 어떤 경우일지 생각해볼 것
            if (this.writingHead == null)
            {
                // We need to allocate memory to write since nobody has written before
                BufferSegment newSegment = this.AllocateSegment(sizeHint);

                // Set all the pointers
                this.writingHead = this.readHead = this.readTail = newSegment;
                this.lastExaminedIndex = 0;
            }
            else
            {
                var bytesLeftInBuffer = this.writingHeadMemory.Length;

                if (bytesLeftInBuffer == 0 || bytesLeftInBuffer < sizeHint)
                {
                    if (this.writingHeadBytesBuffered > 0)
                    {
                        // Flush buffered data to the segment
                        this.writingHead.End += this.writingHeadBytesBuffered;
                        this.writingHeadBytesBuffered = 0;
                    }

                    BufferSegment newSegment = this.AllocateSegment(sizeHint);

                    this.writingHead.SetNext(newSegment);
                    this.writingHead = newSegment;
                }
            }
        }

        // Segment 꺼내와서 할당
        private BufferSegment AllocateSegment(int sizeHint)
        {
            // Segment를 풀에서 꺼내옴
            BufferSegment newSegment = this.CreateSegmentUnsynchronized();

            int maxSize = this.maxPooledBufferSize;
            if (this.pool != null && sizeHint <= maxSize)
            {
                // Use the specified pool as it fits
                newSegment.SetOwnedMemory(this.pool.Rent(this.GetSegmentSize(sizeHint, maxSize)));
            }
            else
            {
                // Use the array pool
                int sizeToRequest = this.GetSegmentSize(sizeHint);
                newSegment.SetOwnedMemory(ArrayPool<byte>.Shared.Rent(sizeToRequest));
            }

            this.writingHeadMemory = newSegment.AvailableMemory;

            return newSegment;
        }

        private int GetSegmentSize(int sizeHint, int maxBufferSize = int.MaxValue)
        {
            // First we need to handle case where hint is smaller than minimum segment size
            sizeHint = Math.Max(this.minimumSegmentSize, sizeHint);
            // After that adjust it to fit into pools max buffer size
            int adjustedToMaximumSize = Math.Min(maxBufferSize, sizeHint);
            return adjustedToMaximumSize;
        }
        
        // Segment를 풀에서 꺼내오기(풀에 없으면 새로 만듦)
        private BufferSegment CreateSegmentUnsynchronized()
        {
            if (this.bufferSegmentPool.TryPop(out var segment))
            {
                return segment;
            }

            return new BufferSegment();
        }
        
        // 다 읽은 Segment를 풀로 반환하기
        private void ReturnSegmentUnsynchronized(BufferSegment segment)
        {
            Debug.Assert(segment != this.readHead, "Returning _readHead segment that's in use!");
            Debug.Assert(segment != this.readTail, "Returning _readTail segment that's in use!");
            Debug.Assert(segment != this.writingHead, "Returning _writingHead segment that's in use!");
            if (this.bufferSegmentPool.Count < MaxSegmentPoolSize)
            {
                this.bufferSegmentPool.Push(segment);
            }
        }

        public bool Flush()
        {
            return CommitUnsynchronized();
        }

        internal bool CommitUnsynchronized()
        {
            this.operationState.EndWrite();

            if (this.notFlushedBytes == 0)
            {
                // 커밋할 데이터가 써져있지 않음
                return true;
            }
            
            // writingHead 업데이트
            Debug.Assert(this.writingHead != null);
            this.writingHead.End += this.writingHeadBytesBuffered;

            // 항상 readTail를 writeHead로 이동시킨다.
            this.readTail = this.writingHead;
            this.readTailIndex = this.writingHead.End;

            var oldLength = this.unconsumedBytes;
            this.unconsumedBytes += this.notFlushedBytes;

            // 리더가 complete 된거면 리셋 안 함
            if (this.pauseWriterThreshold > 0 &&
                oldLength < this.pauseWriterThreshold &&
                this.unconsumedBytes >= this.pauseWriterThreshold &&
                !this.isReaderCompleted)
            {
                this.operationState.PauseWrite();
            }

            this.notFlushedBytes = 0;
            this.writingHeadBytesBuffered = 0;

            if (unconsumedBytes >= this.targetBytes)
            {
                this.Callback.ReadSignal.Set();
            }

            return false;
        }
        
        internal void Advance(int bytesWritten)
        {
            if (this.isReaderCompleted)
            {
                return;
            }
            this.notFlushedBytes += bytesWritten;
            this.writingHeadBytesBuffered += bytesWritten;
            this.writingHeadMemory = this.writingHeadMemory[bytesWritten..];
        }
        internal void AdvanceReader(in SequencePosition consumed)
        {
            this.AdvanceReader(consumed, consumed);
        }
        internal void AdvanceReader(in SequencePosition consumed, in SequencePosition examined)
        {
            this.AdvanceReader((BufferSegment?)consumed.GetObject(), consumed.GetInteger(),
                (BufferSegment?)examined.GetObject(), examined.GetInteger());
        }
        
        private void AdvanceReader(BufferSegment? consumedSegment, int consumedIndex, BufferSegment? examinedSegment, int examinedIndex)
        {
            BufferSegment? returnStart = null;
            BufferSegment? returnEnd = null;
            
            if (examinedSegment != null && this.lastExaminedIndex >= 0)
            {
                var examinedBytes = BufferSegment.GetLength(this.lastExaminedIndex, examinedSegment, examinedIndex);
                var oldLength = this.unconsumedBytes;

                this.unconsumedBytes -= examinedBytes;

                // 절대위치를 저장
                this.lastExaminedIndex = examinedSegment.RunningIndex + examinedIndex;

                Debug.Assert(this.unconsumedBytes >= 0, "Length has gone negative");

                if (oldLength >= this.resumeWriterThreshold &&
                    this.unconsumedBytes < this.resumeWriterThreshold)
                {
                    this.operationState.ResumeWrite();
                }
            }

            if (consumedSegment != null)
            {

                returnStart = this.readHead;
                returnEnd = consumedSegment;
                
                if (consumedIndex == returnEnd.Length)
                {
                    if (this.writingHead != returnEnd)
                    {
                        this.MoveReturnEndToNextBlock(ref returnEnd);
                    }
                    // If the writing head is the same as the block to be returned, then we need to make sure
                    // there's no pending write and that there's no buffered data for the writing head
                    else if (this.writingHeadBytesBuffered == 0 && !this.operationState.IsWritingActive)
                    {
                        // writing head가 return block이면 null로 초기화, 모두 consumed한 것임
                        this.writingHead = null;
                        this.writingHeadMemory = default;

                        this.MoveReturnEndToNextBlock(ref returnEnd);
                    }
                    else
                    {
                        this.readHead = consumedSegment;
                        this.readHeadIndex = consumedIndex;
                    }
                }
                else
                {
                    this.readHead = consumedSegment;
                    this.readHeadIndex = consumedIndex;
                }
                
            }
            
            while (returnStart != null && returnStart != returnEnd)
            {
                var next = returnStart.NextSegment;
                returnStart.ResetMemory();
                this.ReturnSegmentUnsynchronized(returnStart);
                returnStart = next;
            }

            this.Callback.WriteSignal.Set();
            this.operationState.EndRead();
        }

        private void MoveReturnEndToNextBlock(ref BufferSegment? returnEnd)
        {
            BufferSegment? nextBlock = returnEnd!.NextSegment;
            if (this.readTail == returnEnd)
            {
                this.readTail = nextBlock;
                this.readTailIndex = 0;
            }

            this.readHead = nextBlock;
            this.readHeadIndex = 0;

            returnEnd = nextBlock;
        }

        internal void CompleteWriter()
        {
            var completed = this.isReaderCompleted;

            this.CommitUnsynchronized();

            if (this.isWriterCompleted == false)
            {
                this.isWriterCompleted = true;
            }

            if (completed)
            {
                this.CompleteMadline();
            }
        }

        internal void CompleteReader()
        {
            var completed = this.isWriterCompleted;

            if (this.operationState.IsReadingActive)
            {
                this.operationState.EndRead();
            }

            if (this.isReaderCompleted == false)
            {
                this.isReaderCompleted = true;
            }
            
            if (completed)
            {
                this.CompleteMadline();
            }

        }

        private void CompleteMadline()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            // 모든 세그먼트를 반환
            var segment = this.readHead ?? this.readTail;
            while (segment != null)
            {
                BufferSegment returnSegment = segment;
                segment = segment.NextSegment;

                returnSegment.ResetMemory();
            }

            this.ClearReservedRead();
            this.ClearReservedWrite();
            this.writingHead = null;
            this.readHead = null;
            this.readTail = null;
            this.lastExaminedIndex = -1;
        }

        private void ClearReservedRead()
        {
            this.operationState.EndReserveRead();
        }
        private void ClearReservedWrite()
        {
            this.operationState.EndReserveWrite();
        }
        
        public void Reset()
        {
            this.disposed = false;
            this.ResetState();
        }
        
    }
}
