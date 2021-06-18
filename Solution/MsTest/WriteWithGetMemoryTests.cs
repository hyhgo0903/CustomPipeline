namespace Tests
{
    using MadPipeline;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.IO;
    using System.Threading;

    [TestClass]
    public sealed class WriteWithGetMemoryTests : MadlineTest
    {
        private readonly Madline madline;
        private readonly IMadlineWriter madWriter;
        private readonly IMadlineReader madReader;

        private int writeTimes;
        private int readTimes;
        private long writtenBytes;
        private long readBytes;

        public WriteWithGetMemoryTests()
        {
            // 기존의 Threshold 가진 madline으로 테스트를 진행했습니다.
            var malineOptions = new MadlineOptions();
            this.madline = new Madline(malineOptions);
            this.madWriter = this.madline;
            this.madReader = this.madline;
        }

        // 데이터를 읽도록 시도하고, 실패한 경우 예약

        public void StartWrite()
        {
            while (this.writeTimes > 0)
            {
                if (this.madline.Status.IsWritingPaused == false)
                {
                    this.WriteProcess();
                }
                else
                {
                    // 읽기를 대기해준다
                    Thread.Sleep(1);
                }
            }
        }

        public void StartRead()
        {
            while (this.readTimes > 0)
            {
                if (this.madline.Status.IsReadingPaused == false)
                {
                    this.ReadProcess();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public void WriteProcess()
        {
            var number = r.Next(20, 2000);

            var memory = this.madWriter.GetMemory(number + 2);

            var rawSource = CreateMessageWithRandomBody(number);
            rawSource.CopyTo(memory);
            Interlocked.Add(ref this.writeTimes, -1);
            if (this.madWriter.TryAdvance(number + 2))
            {
                this.writtenBytes += number + 2;
                this.madWriter.Flush();
            }
            else
            {
                this.madWriter.WriteSignal().OnCompleted(
                    () =>
                    {
                        this.WriteProcess();
                    });
            }
        }
        // WriteProcess()를 통해 기록된 것을 읽고 구문분석
        public void ReadProcess()
        {
            var resultInt = this.madReader.TryRead(out var result);
            if (resultInt > 0)
            {
                Interlocked.Add(ref this.readTimes, -1);
                this.readBytes += result.Length;
                this.madReader.AdvanceTo(result.End);
                if (resultInt == 1)
                {
                    // 아직 읽을 게 남은 경우이므로 다시 읽기 시도
                    this.ReadProcess();
                }
            }
            else
            {
                this.madReader.DoRead().Then(
                    readResult =>
                    {
                        Interlocked.Add(ref this.readTimes, -1);
                        this.readBytes += readResult.Length;
                        this.madReader.AdvanceTo(readResult.End);
                    });
            }
        }

        [DataRow(10)]
        [DataRow(100)]
        [DataRow(1000)]
        [DataRow(10000)]
        [TestMethod]
        public void MassiveGetMemoryTest(int times)
        {
            this.writeTimes = times;
            this.readTimes = times;
            var writeThread = new Thread(this.StartWrite);
            var readThread = new Thread(this.StartRead);
            readThread.Start();
            writeThread.Start();
            readThread.Join();
            writeThread.Join();
            using (var readFile = new StreamWriter(@"..\MassiveGetMemoryTest.txt", true))
            {
                readFile.WriteLine("Times = {0}, writtenBytes = {1}, readBytes = {2}",
                    times, this.writtenBytes, this.readBytes);
            }
            Assert.AreEqual(this.writtenBytes, this.readBytes);
        }

    }
}