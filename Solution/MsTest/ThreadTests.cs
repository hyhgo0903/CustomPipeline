﻿using System.Threading;
using MadPipeline;

namespace Tests
{
    using System.Buffers;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Infrastructure;

    [TestClass]
    public sealed class ThreadTests : MadlineTest
    {

        private int writeProcessPassed;
        private int readProcessPassed;
        Thread writeThread;
        Thread readThread;

        public ThreadTests()
        {
            this.writeThread = new Thread(this.WriteProcess);
            this.readThread = new Thread(this.ReadProcess);
        }


        // 데이터를 읽도록 시도하고, 실패한 경우 예약
        public void WriteProcess()
        {
            ++this.writeProcessPassed;
            var rawSource = CreateMessage(Encoding.ASCII.GetBytes("Hello World!"));
            if (this.MadWriter.TryWrite(rawSource) == false)
            {
                // TryWrite에 실패한다면 이 함수를 액션으로 예약
                this.MadWriter.WriteSignal().OnCompleted(
                    () =>
                    {
                        this.WriteProcess();
                    });
            }
        }
        // WriteProcess()를 통해 기록된 것을 읽고 구문분석
        public void ReadProcess()
        {
            ++this.readProcessPassed;
            var resultInt = this.MadReader.TryRead(out var result);
            if (resultInt != 0)
            {
                var message = GetBodyFromMessage(result);
                Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(message));
                Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(message));
                Assert.AreEqual(12, GetBodyLengthFromMessage(result));
                this.MadReader.AdvanceTo(result.End);
                if (resultInt == 1)
                {
                    // 아직 읽을 게 남은 경우이므로 다시 읽기 시도
                    this.ReadProcess();
                }
            }
            else
            {
                this.MadReader.DoRead().Then(
                    readResult =>
                    {
                        var message = GetBodyFromMessage(readResult);
                        Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(message));
                        Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(message));
                        Assert.AreEqual(12, GetBodyLengthFromMessage(readResult));
                        this.MadReader.AdvanceTo(readResult.End);
                        this.ReadProcess();
                    });
            }
        }


        [TestMethod]
        public void WriteWithReadTest()
        {
            this.writeThread.Start();
            this.readThread.Start();
            this.writeThread.Join();
            this.readThread.Join();
            Assert.AreEqual(1, this.writeProcessPassed);
        }

        [TestMethod]
        public void FirstWriteTest()
        {
            this.writeThread.Start();
            Thread.Sleep(10);
            this.readThread.Start();
            this.writeThread.Join();
            this.readThread.Join();
            Assert.AreEqual(1, this.readProcessPassed);
            Assert.AreEqual(1, this.writeProcessPassed);
        }
        
        [TestMethod]
        public void FirstReadTest()
        {
            this.readThread.Start();
            Thread.Sleep(10);
            this.writeThread.Start();
            this.writeThread.Join();
            this.readThread.Join();
            Assert.AreEqual(2, this.readProcessPassed);
            Assert.AreEqual(1, this.writeProcessPassed);
        }

    }
}