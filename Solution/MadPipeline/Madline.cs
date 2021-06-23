namespace MadPipeline
{
    using MadngineSource;
    using System;
    using System.Buffers;
    using System.Diagnostics;
    using System.Threading;

    public sealed class Madline : IMadlineReader, IMadlineWriter
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
        
        private BufferSegment? readTail;
        private int readTailIndex;

        private readonly int maxPooledBufferSize;
        private bool disposed;

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
        private readonly object sync;

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
            this.Callback = new MadlineCallbacks();
            this.sync = new object();

            //this.syncEvents = new SyncEvents();
            //this.Producer = new Producer(this.syncEvents, this.operationState);
            //this.Consumer = new Consumer(this.syncEvents, this.operationState);
        }

        public long Length => this.unconsumedBytes;

        public MadlineOperationState State => this.operationState;
        
        public MadlineCallbacks Callback { get; }

        public ReadOnlySequence<byte> ReadBuffer
            => (this.readHead == null) || (this.readTail == null) || this.unconsumedBytes <= 0 ? default
                : new ReadOnlySequence<byte>(
                    this.readHead, this.readHeadIndex,
                    this.readTail, this.readTailIndex);
        #region Thread

        // 2개의 쓰레드 이용 
        // 쓰기부 읽기부 메서드별로 구분되도록 지원해야

        // 일단 스레드 region에서 수정 중이라 순서는 완료되고 나서 정상화

        //private Consumer Consumer { get; }
        //private Producer Producer { get; }
        //private SyncEvents syncEvents;

        //private bool CheckWriterThread()
        //{
        //    return Thread.CurrentThread == writerThread;
        //}
        //private bool CheckReaderThread()
        //{
        //    return Thread.CurrentThread == readerThread;
        //}

        #endregion

        #region BufferManagement


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
            this.AllocateWriteHead(sizeHint);
            return this.writingHeadMemory;
        }

        public Span<byte> GetSpan(int sizeHint)
        {
            this.AllocateWriteHead(sizeHint);

            return this.writingHeadMemory.Span;
        }
        
        private void AllocateWriteHead(int sizeHint)
        {
            if (this.operationState.IsWritingPaused && this.writingHeadMemory.Length != 0 &&
                this.writingHeadMemory.Length >= sizeHint)
            {
                return;
            }

            lock (this.sync)
            {
                this.operationState.BeginWrite();

                if (this.writingHead == null)
                {
                    // We need to allocate memory to write since nobody has written before
                    BufferSegment newSegment = this.AllocateSegment(sizeHint);

                    // Set all the pointers
                    this.writingHead = this.readHead = this.readTail = newSegment;
                    Interlocked.Exchange(ref this.lastExaminedIndex, 0);
                }
                else
                {
                    var bytesLeftInBuffer = this.writingHeadMemory.Length;

                    if (bytesLeftInBuffer != 0 && bytesLeftInBuffer >= sizeHint)
                    {
                        return;
                    }

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
            var newSegment = this.CreateSegment();
            
            if (this.pool != null && sizeHint <= this.maxPooledBufferSize)
            {
                // Use the specified pool as it fits
                newSegment.SetOwnedMemory(this.pool.Rent(this.GetSegmentSize(
                    sizeHint, this.maxPooledBufferSize)));
            }
            else
            {
                // Use the array pool
                var sizeToRequest = this.GetSegmentSize(sizeHint);
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
            return Math.Min(maxBufferSize, sizeHint);
        }
        
        // Segment를 풀에서 꺼내오기(풀에 없으면 새로 만듦)
        private BufferSegment CreateSegment()
        {
            lock (this.sync)
            {
                var popped = this.bufferSegmentPool.TryPop(out var segment) ? segment : new BufferSegment();
                return popped;
            }
        }
        
        // 다 읽은 Segment를 풀로 반환하기
        private void ReturnSegment(BufferSegment segment)
        {
            Debug.Assert(segment != this.readHead, "Returning _readHead segment that's in use!");
            Debug.Assert(segment != this.readTail, "Returning _readTail segment that's in use!");
            Debug.Assert(segment != this.writingHead, "Returning _writingHead segment that's in use!");
            if (this.bufferSegmentPool.Count >= MaxSegmentPoolSize)
            {
                return;
            }

            lock (this.sync)
            {
                this.bufferSegmentPool.Push(segment);
            }
        }

        #endregion

        #region Advance


        public void Advance(int bytesWritten)
        {
            if (this.isReaderCompleted)
            {
                return;
            }
            
            this.notFlushedBytes += bytesWritten;
            this.writingHeadBytesBuffered += bytesWritten;
            this.writingHeadMemory = this.writingHeadMemory[bytesWritten..];
        }

        public void AdvanceTo(in SequencePosition consumed)
        {
            // AdvanceTo의 인자 하나로 단일화
            // examinedSegment == consumedSegment
            // examinedIndex == consumedIndex


            // 일단 락종류는 Monitor로 하고, 성능 측정하며 SpinLock 고려
            // 일단 Enter Exit만 있지만 락 위치를 fix하면 try finally 구문으로
            // (성능에는 영향이 있으나 예외처리 해줘야 데드락 등의 문제 방지)
            
            var consumedSegment = (BufferSegment?) consumed.GetObject();
            var consumedIndex = consumed.GetInteger();
            
            BufferSegment? returnStart = null;
            BufferSegment? returnEnd = null;
            lock (this.sync)
            {
                if (consumedSegment != null && this.lastExaminedIndex >= 0)
                {
                    var examinedBytes = BufferSegment.GetLength(this.lastExaminedIndex, consumedSegment, consumedIndex);

                    Interlocked.Add(ref this.unconsumedBytes, -examinedBytes);

                    var calculatedExaminedIndex = consumedSegment.RunningIndex + consumedIndex;
                    // 절대위치를 저장
                    Interlocked.Exchange(ref this.lastExaminedIndex, calculatedExaminedIndex);

                    Debug.Assert(this.unconsumedBytes >= 0, "Length has gone negative");

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

                // 세그먼트를 거듭해가며 넘는 과정
                while (returnStart != null && returnStart != returnEnd)
                {
                    var next = returnStart.NextSegment;
                    returnStart.ResetMemory();
                    this.ReturnSegment(returnStart);
                    returnStart = next;
                }

                this.operationState.EndRead();
            }


            if (this.State.IsWritingPaused && this.unconsumedBytes < this.resumeWriterThreshold)
            {
                this.operationState.ResumeWrite();
                this.Callback.WriteSignal.Set();
                this.Callback.WriteSignal.Reset();
            }

        }

        private void MoveReturnEndToNextBlock(ref BufferSegment? returnEnd)
        {
            var nextBlock = returnEnd!.NextSegment;
            if (this.readTail == returnEnd)
            {
                this.readTail = nextBlock;
                this.readTailIndex = 0;
            }

            this.readHead = nextBlock;
            this.readHeadIndex = 0;

            returnEnd = nextBlock;
        }

        #endregion

        #region Complete

        public void CompleteWriter()
        {
            var completed = this.isReaderCompleted;

            this.Flush();

            lock (this.sync)
            {
                this.isWriterCompleted = true;

                if (completed)
                {
                    this.CompleteMadline();
                }
            }
        }

        public void CompleteReader()
        {
            var completed = this.isWriterCompleted;
            
            lock (this.sync)
            {
                if (this.operationState.IsReadingActive)
                {
                    this.operationState.EndRead();
                }

                this.isReaderCompleted = true;

                if (completed)
                {
                    this.CompleteMadline();
                }
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

            this.writingHead = null;
            this.readHead = null;
            this.readTail = null;
            this.lastExaminedIndex = -1;
        }

        #endregion

        #region Write
        
        public bool TryWrite(in ReadOnlyMemory<byte> source)
        {
            // unconsumedBytes가 PauseWriterThreshold를 넘고있는 경우임
            if (this.operationState.IsWritingPaused)
            {
                return false;
            }
            if (this.isWriterCompleted)
            {
                throw new Exception("No Writing Allowed");
            }
            
            this.AllocateWriteHead(0);

            this.CopyToWriteHead(in source);

            this.Flush();

            return true;
        }

        public bool TryAdvance(int bytes)
        {
            if (this.operationState.IsWritingPaused)
            {
                return false;
            }

            if (this.isWriterCompleted)
            {
                throw new Exception("No Writing Allowed");
            }

            this.Advance(bytes);
            this.Flush();
            return true;
        }

        public bool WriteCheck()
        {
            if (this.operationState.IsWritingPaused)
            {
                return false;
            }

            if (this.isWriterCompleted)
            {
                throw new Exception("No Writing Allowed");
            }

            return true;
        }


        public void CopyToWriteHead(in ReadOnlyMemory<byte> source)
        {
            lock (this.sync)
            {
                var sourceLength = source.Length;
                if (sourceLength <= this.writingHeadMemory.Length)
                {
                    // 세그먼트 하나에 쓰는 경우
                    source.CopyTo(this.writingHeadMemory);

                    this.Advance(sourceLength);
                }
                else
                {
                    // 세그먼트를 넘어가며 써야하는 경우
                    Debug.Assert(this.writingHead != null);
                    var sourceSpan = source.Span;
                    var destination = this.writingHeadMemory.Span;

                    while (true)
                    {
                        var writable = Math.Min(destination.Length, sourceSpan.Length);
                        sourceSpan[..writable].CopyTo(destination);
                        sourceSpan = sourceSpan[writable..];
                        this.Advance(writable);

                        if (sourceSpan.Length == 0)
                        {
                            break;
                        }

                        this.writingHead.End += writable;
                        Interlocked.Exchange(ref this.writingHeadBytesBuffered, 0);

                        // This is optimized to use pooled memory. That's why we pass 0 instead of
                        // source.Length
                        BufferSegment newSegment = this.AllocateSegment(0);

                        this.writingHead.SetNext(newSegment);
                        this.writingHead = newSegment;

                        destination = this.writingHeadMemory.Span;
                    }
                }
            }
        }

        public Signal WriteSignal()
        {
            return this.Callback.WriteSignal;
        }

        public Signal DoAdvance(int bytes)
        {
            this.Advance(bytes);
            this.Flush();
            return this.Callback.WriteSignal;
        }

        #endregion

        #region Read

        // 변수로 넣는 targetLength는 DecodingFramer로 헤더 분석 후 사이즈 전달은 인자를 넣는다고 가정
        public bool TryRead(out ReadOnlySequence<byte> result, int targetLength)
        {
            lock (this.sync)
            {
                if (this.unconsumedBytes <= 0
                || this.readHead == null || this.readTail == null)
                {
                    this.operationState.PauseRead();
                }
                if (this.operationState.IsReadingPaused)
                {
                    this.targetBytes = targetLength;
                    result = default;
                    return false;
                }

                if (this.isReaderCompleted)
                {
                    throw new Exception("Reader is Completed");
                }
                
                if (this.unconsumedBytes >= targetLength)
                {
                    this.GetReadResult(out result);
                    // 더 쓸 데이터가 없으면 읽기를 완료(2)
                    // 아니면 또 쓰기 시도할 예정으로 1 return (읽는건 중단 없이 끝까지 읽는다)
                    return true;
                }

                this.targetBytes = targetLength;
                this.operationState.PauseRead();
                result = default;
                return false;
            }
        }
        
        // 예약 대기
        public Future<ReadOnlySequence<byte>> DoRead()
        {
            this.Callback.ResetReadPromise();
            return this.Callback.ReadPromise.GetFuture();
        }

        private void GetReadResult(out ReadOnlySequence<byte> result)
        {
            var head = this.readHead;
            if (head != null)
            {
                Debug.Assert(this.readTail != null);
                
                // 잘라서 제공하는 버전
                result = new ReadOnlySequence<byte>(head,
                        this.readHeadIndex,
                        this.readTail,
                        this.readTailIndex);
            }
            else
            {
                result = default;
            }

            this.operationState.BeginRead();
        }

        #endregion

        #region Reset

        // 당장은 안 쓰고 있으나, 재사용을 위해 삭제하지는 않았음
        public void Reset()
        {
            lock (this.sync)
            {
                this.disposed = false;
                this.operationState = default;
                this.isWriterCompleted = false;
                this.isReaderCompleted = false;
                this.readTailIndex = 0;
                this.readHeadIndex = 0;
                this.lastExaminedIndex = -1;
                this.notFlushedBytes = 0;
                this.unconsumedBytes = 0;
            }
        }

        #endregion
        
        public bool Flush()
        {
            lock (this.sync)
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

                if (this.pauseWriterThreshold > 0 &&
                    oldLength < this.pauseWriterThreshold &&
                    this.unconsumedBytes >= this.pauseWriterThreshold &&
                    !this.isReaderCompleted)
                {
                    this.operationState.PauseWrite();
                }

                this.notFlushedBytes = 0;
                this.writingHeadBytesBuffered = 0;

                // 프라미스 캡쳐해서 쓰는것을 고민해볼 것
                //Promise<ReadOnlySequence<byte>>? promise = null;

                // 타겟바이트 이상으로 들어온 경우 예약된 읽기명령을 실행
                if (this.operationState.IsReadingPaused)
                {
                    if (this.unconsumedBytes >= this.targetBytes)
                    {
                        this.operationState.ResumeRead();
                        this.GetReadResult(out var result);
                        this.Callback.ReadPromise.Complete(result);
                        this.Callback.ResetReadPromise();
                    }
                }
            }

            return false;
        }

    }
}
