namespace MadPipeline
{
    using System;
    using System.Buffers;
    using System.Diagnostics;
    using MadngineSource;

    public sealed partial class Madline
    {
        public bool TryRead(out ReadResult result, int targetLength = -1)
        {
            if (this.isReaderCompleted)
            {
                throw new Exception("Reader is Completed");
            }

            if (this.unconsumedBytes > 0)
            {
                this.GetReadResult(out result, targetLength);
                return true;
            }

            result = default;
            return false;
        }


        public Future<ReadResult> DoRead()
        {
            var promise = new Promise<ReadResult>();
            this.Callback.ReadPromise = promise;
            return promise.GetFuture();
        }
        
        private void GetReadResult(out ReadResult result, int targetLength)
        {
            var isCompleted = this.isWriterCompleted;
            var head = this.readHead;
            if (head != null)
            {
                Debug.Assert(this.readTail != null);
                // Reading commit head shared with writer

                var readOnlySequence = new ReadOnlySequence<byte>(head,
                    this.readHeadIndex,
                    this.readTail,
                    this.readTailIndex);
                var length = BufferSegment.GetLength(head, this.readHeadIndex,
                    this.readTail, this.readTailIndex);

                // target길이가 버퍼길이보다 작고, target길이가 유효하게(0이상) 입력된 경우라면 잘라서 일부만 제공
                readOnlySequence = length > targetLength && targetLength > 0 ?
                    readOnlySequence.Slice(0, targetLength) : readOnlySequence;
                result = new ReadResult(readOnlySequence, isCompleted);
            }
            else
            {
                result = new ReadResult(default, isCompleted);
            }

            this.operationState.BeginRead();
        }

        public bool TryWrite(ReadOnlyMemory<byte> source, int targetLength = -1)
        {
            // unconsumedBytes가 PauseWriterThreshold를 넘고있는 경우임
            if (this.operationState.IsWritingPaused)
            {
                return false;
            }
            if (targetLength >= 0)
            {
                this.targetBytes = targetLength;
            }
            if (this.isWriterCompleted)
            {
                throw new Exception("No Writing Allowed");
            }

            this.AllocateWriteHeadIfNeeded(0);
            
            if (source.Length <= this.writingHeadMemory.Length)
            {
                source.CopyTo(this.writingHeadMemory);

                this.Advance(source.Length);
            }
            else
            {
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

                    // We filled the segment
                    this.writingHead.End += writable;
                    this.writingHeadBytesBuffered = 0;

                    // This is optimized to use pooled memory. That's why we pass 0 instead of
                    // source.Length
                    BufferSegment newSegment = this.AllocateSegment(0);

                    this.writingHead.SetNext(newSegment);
                    this.writingHead = newSegment;

                    destination = this.writingHeadMemory.Span;
                }
            }

            this.Flush();

            return true;
        }

        public Signal DoWrite()
        {                   
            this.Callback.WriteSignal.Reset();
            return this.Callback.WriteSignal;
        }
    }

}
