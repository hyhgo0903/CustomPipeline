//namespace Tests
//{
//    using MadPipeline;
//    using Microsoft.VisualStudio.TestTools.UnitTesting;
//    using System.Buffers;
//    using System.Diagnostics;
//    using System.IO;
//    using System.Threading;

//    [TestClass]
//    public sealed class BaseOnCapacity : MadlineTest
//    {
//        private readonly Madline madline;
//        private readonly IMadlineWriter madWriter;
//        private readonly IMadlineReader madReader;

//        private int leftoverBytes;
//        private long writtenBytes;
//        private long readBytes;
//        private bool endReader;

//        public BaseOnCapacity()
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
//            while (this.leftoverBytes > 0)
//            {
//                if (this.madline.State.IsWritingPaused == false)
//                {
//                    this.WriteProcess();
//                }
//            }
//        }

//        public void StartRead()
//        {
//            do
//            {
//                if (this.madline.State.IsReadingPaused == false)
//                {
//                    this.ReadProcess();
//                }
//            } while (this.endReader == false);
//        }

//        public void WriteProcess()
//        {
//            // 평균적으로 1kb
//            if (this.madWriter.TryAdvance())
//            {
//                var number = r.Next(20, 2000);
//                // 헤더 포함해서 이정도 이하 남으면 딱코딱뎀 맞춰준다
//                if (this.leftoverBytes < 2002)
//                {
//                    number = this.leftoverBytes - 2;
//                }
//                var rawSource = CreateMessageWithRandomBody(number);
//                this.madWriter.GetMemory();
//                this.madWriter.CopyToWriteHead(in rawSource);
//                this.madWriter.Flush();
//                this.writtenBytes += number + 2;
//                Interlocked.Add(ref this.leftoverBytes, -number-2);
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
//            this.readBytes += result.Length;
//            this.madReader.AdvanceTo(result.End);
//        }
        
//        // 1GB
//        [DataRow(1073741824)]
//        [TestMethod]
//        public void BaseOnCapacityTest(int capacity)
//        {
//            var sw = new Stopwatch();
//            sw.Start();

//            this.leftoverBytes = capacity;
//            var writeThread = new Thread(this.StartWrite);
//            var readThread = new Thread(this.StartRead);
//            readThread.Start();
//            writeThread.Start();
//            writeThread.Join();
//            this.madWriter.CompleteWriter();
//            this.endReader = true;
//            readThread.Join();
//            sw.Stop();
//            using (var readFile = new StreamWriter(@"..\1GBTest.txt", true))
//            {
//                readFile.WriteLine("Capacity = {0},\twrittenBytes = {1},\treadBytes = {2},\nTime = {3} millisecond",
//                    capacity, this.writtenBytes, this.readBytes, sw.ElapsedMilliseconds);
//            }
//            Assert.AreEqual(capacity, this.readBytes);
//        }
//    }
//}