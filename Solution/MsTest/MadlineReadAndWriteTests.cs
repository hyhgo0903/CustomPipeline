using System;
using System.Buffers;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Infrastructure;

namespace Tests
{
    [TestClass]
    public sealed class MadlineCallbackTests : MadlineTest
    {
        private int writeProcessPassed;
        private int readProcessPassed;


        // 데이터를 읽도록 시도하고, 실패한 경우 예약
        public void WriteProcess()
        {
            ++writeProcessPassed;
            var received = Encoding.ASCII.GetBytes("Hello World!");
            if (this.Madline.TryWrite(received) == false)
            {
                this.Madline.DoWrite().OnCompleted(
                    () =>
                    {
                        this.WriteProcess();
                    });
            }
        }

        // WriteProcess()를 통해 기록된 것을 읽고 구문분석
        public void ReadProcess()
        {
            ++readProcessPassed;
            if (this.Madline.TryRead(out var result))
            {
                Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual(12, result.Buffer.Length);
                this.Madline.Reader.Advance(result.Buffer.End);
            }
            else
            {
                this.Madline.DoRead().Then(
                    readResult =>
                    {
                        this.SendToSocket(readResult.Buffer);
                    });
            }
        }

        public void SendToSocket(ReadOnlySequence<byte> buffer)
        {
            this.Madline.Reader.Advance(buffer.End);
            this.ReadProcess();
        }


        [TestMethod]
        public void WriteThenReadTest()
        {
            this.WriteProcess();
            this.ReadProcess();
        }

        [TestMethod]
        public void MultipleWriteThenReadTest()
        {
            this.WriteProcess();
            this.WriteProcess();
            this.Madline.TryRead(out var result);
            Assert.AreEqual("Hello World!Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
            Assert.AreEqual(24, result.Buffer.Length);
            this.Madline.Reader.Advance(result.Buffer.End);
        }
        
        [TestMethod]
        public void WriteWhenBufferIsFullTest()
        {
            this.Madline.Writer.WriteEmpty(MaximumSizeHigh);
            this.Madline.Writer.Flush();

            // 이땐 TryWrite false가 되며, OnComplete 예약만..
            this.WriteProcess();

            this.Madline.TryRead(out var buffer);
            // Advance되며 예약된 시그널(쓰기 프로세스 다시 발동) Set
            this.Madline.Reader.Advance(buffer.Buffer.End);

            this.ReadProcess();

            // 처음 WriteProcess 호출되며 한번, 예약된 WriteProcess가 진행되며 두 번
            Assert.AreEqual(2, writeProcessPassed);
            Assert.AreEqual(1, readProcessPassed);
        }
        
        [TestMethod]
        public void ReadWhenBufferIsEmptyTest()
        {
            this.ReadProcess();

            this.Madline.Writer.WriteEmpty(MaximumSizeHigh);
            this.Madline.Writer.Flush();

            // 이땐 TryWrite false가 되며, OnComplete 예약만
            this.WriteProcess();
            this.Madline.TryRead(out var buffer);
            // Advance되며 예약된 시그널(쓰기 프로세스 다시 발동) Set
            this.Madline.Reader.Advance(buffer.Buffer.End);
            this.ReadProcess();
        }

        //public void ReadProcess()
        //{
        //    if (this.Madline.TryRead(out var result, size))
        //    {
        //        this.SendToSocket(result.Buffer);
        //    }
        //    else
        //    {
        //        this.Madline.DoRead(size)
        //            .Then(result =>
        //            {
        //                this.SendToSocket(result.Buffer);
        //            });
        //    }
        //}

    }
}