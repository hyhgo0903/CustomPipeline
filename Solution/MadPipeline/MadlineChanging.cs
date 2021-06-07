namespace MadPipeline
{
    using System;
    using System.Buffers;
    using System.Diagnostics;
    using MadngineSource;

    public sealed partial class Madline
    {
        // 타겟바이트 이 이하 쓰려고하면, 실패한다.
        public bool TryRead(out ReadResult result, int targetLength = -1)
        {
            if (this.isReaderCompleted)
            {
                throw new Exception("Reader is Completed");
            }

            if (this.unconsumedBytes > 0)
            {
                this.GetReadResult(out result);
                return true;
            }

            result = default;
            return false;
        }


        public Promise<ReadResult> DoRead()
        {
            this.Callback.ReadPromise = new Promise<ReadResult>();
            return this.Callback.ReadPromise;
        }

        private void GetReadResult(out ReadResult result)
        {
            var isCompleted = this.isWriterCompleted;

            // No need to read end if there is no head
            var head = this.readHead;
            // null이 아닌 경우가 있는가?
            if (head != null)
            {
                Debug.Assert(this.readTail != null);
                // Reading commit head shared with writer
                var readOnlySequence = new ReadOnlySequence<byte>(head,
                    this.readHeadIndex,
                    this.readTail,
                    this.readTailIndex);
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
            if (this.operationState.IsWritingPaused)
            {
                return false;
            }
            if (targetLength != -1)
            {
                this.targetBytes = targetLength;
            }
            if (isWriterCompleted)
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

            this.CommitUnsynchronized();

            return true;
        }

        public Signal DoWrite()
        {                   
            this.Callback.WriteSignal.Reset();
            return this.Callback.WriteSignal;
        }
    }

}
