using System;
using System.Buffers;
using System.Diagnostics;
using System.Net.Sockets;
using MadPipeline.MadngineSource;

namespace MadPipeline
{
    public sealed partial class Madline
    {
        // 타겟바이트 이 이하 쓰려고하면, 실패한다.
        public bool TryRead(out ReadResult result, int targetLength = -1)
        {
            if (targetLength == -1)
            {
                targetLength = this.targetBytes;
            }
            if (isReaderCompleted)
            {
                throw new Exception("Reader is Completed");
            }

            if (unconsumedBytes > 0 && unconsumedBytes >= targetLength)
            {
                GetReadResult(out result);
                return true;
            }

            result = default;
            return false;
        }


        public Future<ReadResult> DoRead(out ReadResult result)
        {
            var promise = new Promise<ReadResult>();
            GetReadResult(out result);
            promise.Complete(result);

            return promise.GetFuture();
        }

        private void GetReadResult(out ReadResult result)
        {
            var isCompleted = isWriterCompleted;

            // No need to read end if there is no head
            var head = readHead;
            if (head != null)
            {
                Debug.Assert(readTail != null);
                // Reading commit head shared with writer
                var readOnlySequence = new ReadOnlySequence<byte>(head, readHeadIndex, readTail, readTailIndex);
                result = new ReadResult(readOnlySequence, isCompleted);
            }
            else
            {
                result = new ReadResult(default, isCompleted);
            }

            operationState.BeginRead();
        }

        public bool TryWrite(in ReadOnlyMemory<byte> source, int targetLength = -1)
        {
            if (targetLength == -1)
            {
                targetLength = this.targetBytes;
            }
            var sourceLength = source.Length;
            if (sourceLength < targetLength)
            {
                return false;
            }

            if (sourceLength + unconsumedBytes > pauseWriterThreshold)
            {
                return false;
            }

            DoWrite(in source);
            return true;
        }

        public Signal DoWrite(in ReadOnlyMemory<byte> source)
        {
            var signal = new Signal();
            if (isWriterCompleted)
            {
                throw new Exception("No Writing Allowed");
            }

            AllocateWriteHeadIfNeeded(0);

            if (source.Length <= writingHeadMemory.Length)
            {
                source.CopyTo(writingHeadMemory);

                Advance(source.Length);
            }
            else
            {
                Debug.Assert(writingHead != null);
                var sourceSpan = source.Span;
                var destination = writingHeadMemory.Span;

                while (true)
                {
                    int writable = Math.Min(destination.Length, sourceSpan.Length);
                    sourceSpan[..writable].CopyTo(destination);
                    sourceSpan = sourceSpan[writable..];
                    Advance(writable);

                    if (sourceSpan.Length == 0)
                    {
                        break;
                    }

                    // We filled the segment
                    writingHead.End += writable;
                    writingHeadBytesBuffered = 0;

                    // This is optimized to use pooled memory. That's why we pass 0 instead of
                    // source.Length
                    BufferSegment newSegment = AllocateSegment(0);

                    writingHead.SetNext(newSegment);
                    writingHead = newSegment;

                    destination = writingHeadMemory.Span;
                }
            }

            CommitUnsynchronized();

            signal.Set();

            return signal;
        }

        /*

    // 사용례
    class Example
    {
        private Socket socket;

        private Pipeline pipeline = new();
        // ...
        public void ProcessReceive1()
        {
            // 메모리를 가온다
            var memory = this.pipeline.GetMemory(1);
            this.receiveArgs.SetBuffer = memory;
            var received = this.socket.ReceiveAsync(this.receiveArgs);
            // 대충 소켓으로부터 받는 코드
            var signal = this.pipeline.Advance(received);
            signal.OnComplete(() =>
            {
                // 시그널이 오면(promise) 하고 다시 이 함수위로 돌아온다.
                this.ProcessReceive1();
            });
        }


        public void ProcessReceive2()
        {
            // 메모리를 가온다
            var memory = this.pipeline.GetMemory(1);
            this.receiveArgs.SetBuffer = memory;
            var received = this.socket.ReceiveAsync(this.receiveArgs);
            // 대충 소켓으로부터 받는 코드
            // 쓰기 시도
            if (this.pipeline.TryAdvance(received) == false)
            {
                // 실패한 경우 달아둔다. (Promise)
                this.pipeline.Advance(received)
                    .OnComplete(() =>
                    {
                        this.ProcessReceive2();
                    });
            }
            // 성공한 경우 다시 Receive하는 프로세스.
            else
            {
                this.ProcessReceive2();
            }
        }




        public void ProcessSend()
        {
            if (this.pipeline.TryRead(var result, size))
            {
                this.SendToSocket(result.Buffer);
            }
            else
            {
                this.pipeline.Read(size)
                    .Then(result =>
                    {
                        this.SendToSocket(result.Buffer);
                    });
            }
        }
        // result 로 pipe가 완료되었는지 캔슬되었는지 등등 검사 후 
        //스트림 내용 그대로 소켓에 Send 하는 작업
        public void SendToSocket(Buffer buffer)
        {
            //....
            this.pipeline.AdvanceTo(result.Buffer.End);
            this.ProcessSend();
        }
        */
    }

}
