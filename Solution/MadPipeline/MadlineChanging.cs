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
            if (this.isReaderCompleted)
            {
                throw new Exception("Reader is Completed");
            }

            if (this.unconsumedBytes > 0)
            {
                this.GetReadResult(out result);
                this.Reader.Advance(result.Buffer.End);
                return true;
            }

            result = default;
            return false;
        }


        public Future<ReadResult> DoRead()
        {
            var promise = new Promise<ReadResult>();
            this.Callback.ReadPromise = promise;
            return this.Callback.ReadPromise.GetFuture();
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
            if (targetLength == -1)
            {
                targetLength = this.targetBytes;
            }
            var sourceLength = source.Length;
            if (sourceLength < targetLength)
            {
                return false;
            }

            if (sourceLength + this.unconsumedBytes > this.pauseWriterThreshold)
            {
                return false;
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
            return this.Callback.WriteSignal;
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
                // 시그널이 오면 다시 이 함수위로 돌아온다.
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
