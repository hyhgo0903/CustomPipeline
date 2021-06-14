using System.Threading;

namespace Tests
{
    using System.Buffers;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Infrastructure;

    [TestClass]
    public sealed class ReadAndWriteWithThreadTests : MadlineTest
    {
        private int writeProcessPassed;
        private int readProcessPassed;

        // 데이터를 읽도록 시도하고, 실패한 경우 예약
        public void WriteProcess()
        {
            ++this.writeProcessPassed;
            var received = Encoding.ASCII.GetBytes("Hello World!");
            if (this.MadWriter.TryWrite(received) == false)
            {
                // TryWrite에 실패한다면 이 함수를 액션으로 예약
                this.MadWriter.DoWrite(received).OnCompleted(
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
            if (this.MadReader.TryRead(out var result, 12))
            {
                // TryWrite에 성공했을때 result를 이용한 작업을 여기서
                Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual(12, result.Buffer.Length);
                this.MadReader.AdvanceTo(result.Buffer.End);
            }
            else
            {
                // TryWrite에 실패한다면 이 함수를 액션으로 예약
                this.MadReader.DoRead(out result, 12).Then(
                    readResult =>
                    {
                        this.ReadProcess();
                    });
            }
        }

        [TestMethod]
        public void WriteWithReadTest()
        {
            var writeThread = new Thread(this.WriteProcess);
            var readThread = new Thread(this.ReadProcess);
            writeThread.Start();
            readThread.Start();
            writeThread.Join();
            readThread.Join();
            Assert.AreEqual(1, writeProcessPassed);
        }

        [TestMethod]
        public void FirstWriteTest()
        {
            var writeThread = new Thread(this.WriteProcess);
            var readThread = new Thread(this.ReadProcess);
            writeThread.Start();
            Thread.Sleep(10);
            readThread.Start();
            writeThread.Join();
            readThread.Join();
            Assert.AreEqual(1, readProcessPassed);
            Assert.AreEqual(1, writeProcessPassed);
        }
        
        [TestMethod]
        public void FirstReadTest()
        {
            var writeThread = new Thread(this.WriteProcess);
            var readThread = new Thread(this.ReadProcess);
            readThread.Start();
            Thread.Sleep(10);
            writeThread.Start();
            writeThread.Join();
            readThread.Join();
            Assert.AreEqual(2, readProcessPassed);
            Assert.AreEqual(1, writeProcessPassed);
        }

    }
}