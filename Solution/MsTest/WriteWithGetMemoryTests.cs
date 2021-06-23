//namespace Tests
//{
//    using MadPipeline;
//    using Microsoft.VisualStudio.TestTools.UnitTesting;
//    using System.Buffers;
//    using System.Diagnostics;
//    using System.IO;
//    using System.Threading;

//    [TestClass]
//    public sealed class TryAdvanceTests : MadlineTest
//    {
//        private readonly Madline madline;
//        private readonly IMadlineWriter madWriter;
//        private readonly IMadlineReader madReader;

//        private int writeTimes;
//        private int readTimes;
//        private long writtenBytes;
//        private long readBytes;

//        public TryAdvanceTests()
//        {
//            // 기존의 Threshold 가진 madline으로 테스트를 진행했습니다.
//            var malineOptions = new MadlineOptions();
//            this.madline = new Madline(malineOptions);
//            this.madWriter = this.madline;
//            this.madReader = this.madline;
//        }

//        // 데이터를 읽도록 시도하고, 실패한 경우 예약

//        public void StartWrite()
//        {
//            while (this.writeTimes > 0)
//            {
//                // 이러면 콜백쓰는 의미가 있을까 싶기도 하지만
//                // 콜백까지 쓰레드 대기(혹은 죽이기) -> 깨우기(혹은 새 스레드 생성)
//                // 보다 성능상 유리할것으로 판단(최소한 테스트 환경에서)
//                if (this.madline.State.IsWritingPaused == false)
//                {
//                    this.WriteProcess();
//                }
//            }
//        }

//        public void StartRead()
//        {
//            while (this.readTimes > 0)
//            {
//                if (this.madline.State.IsReadingPaused == false)
//                {
//                    this.ReadProcess();
//                }
//            }
//        }

//        public void WriteProcess()
//        {
//            // 평균적으로 1kb
//            if (this.madWriter.TryAdvance())
//            {
//                var number = r.Next(20, 2000);
//                var rawSource = CreateMessageWithRandomBody(number);
//                this.madWriter.GetMemory();
//                this.madWriter.CopyToWriteHead(in rawSource);
//                this.madWriter.Flush();
//                this.writtenBytes += number + 2;
//                Interlocked.Add(ref this.writeTimes, -1);
//            }
//            else
//            {
//                this.madWriter.WriteSignal().OnCompleted(
//                    () =>
//                    {
//                        this.WriteProcess();
//                    });
//            }

//        }
//        // WriteProcess()를 통해 기록된 것을 읽고 구문분석
//        public void ReadProcess()
//        {
//            var resultInt = this.madReader.TryRead(out var result);
//            if (resultInt > 0)
//            {
//                this.SendToSocket(in result);
//                if (resultInt == 1)
//                {
//                    // 아직 읽을 게 남은 경우이므로 다시 읽기 시도
//                    this.ReadProcess();
//                }
//            }
//            else
//            {
//                this.madReader.DoRead().Then(
//                    readResult =>
//                    {
//                        this.SendToSocket(in readResult);
//                    });
//            }
//        }

//        public void SendToSocket(in ReadOnlySequence<byte> result)
//        {
//            Interlocked.Add(ref this.readTimes, -1);
//            this.readBytes += result.Length;
//            this.madReader.AdvanceTo(result.End);
//        }

//        [DataRow(10)]
//        [DataRow(100)]
//        [DataRow(1000)]
//        [DataRow(10000)]
//        [TestMethod]
//        public void MassiveGetMemoryTest(int times)
//        {
//            var sw = new Stopwatch();
//            sw.Start();

//            this.writeTimes = times;
//            this.readTimes = times;
//            var writeThread = new Thread(this.StartWrite);
//            var readThread = new Thread(this.StartRead);
//            readThread.Start();
//            writeThread.Start();
//            readThread.Join();
//            writeThread.Join();
//            sw.Stop();
//            using (var readFile = new StreamWriter(@"..\MassiveGetMemoryTest.txt", true))
//            {
//                readFile.WriteLine("Times = {0},\twrittenBytes = {1},\treadBytes = {2},\nTime = {3} millisecond",
//                    times, this.writtenBytes, this.readBytes, sw.ElapsedMilliseconds);
//            }
//            Assert.AreEqual(this.writtenBytes, this.readBytes);
//        }

//        [TestMethod]
//        [DataRow(10000000)]
//        public void SuperMassiveGetMemoryTest(int times)
//        {
//            var sw = new Stopwatch();
//            sw.Start();

//            this.writeTimes = times;
//            this.readTimes = times;
//            var writeThread = new Thread(this.StartWrite);
//            var readThread = new Thread(this.StartRead);
//            readThread.Start();
//            writeThread.Start();
//            readThread.Join();
//            writeThread.Join();
//            sw.Stop();
//            using (var readFile = new StreamWriter(@"..\MassiveGetMemoryTest.txt", true))
//            {
//                readFile.WriteLine("Times = {0},\twrittenBytes = {1},\treadBytes = {2},\nTime = {3} millisecond",
//                    times, this.writtenBytes, this.readBytes, sw.ElapsedMilliseconds);
//            }
//            Assert.AreEqual(this.writtenBytes, this.readBytes);
//        }
//    }
//}