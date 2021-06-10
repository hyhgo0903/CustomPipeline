namespace Tests
{
    using System.Buffers;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Infrastructure;

    [TestClass]
    public sealed class ReadAndWriteWithCallbackTests : MadlineTest
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
        public void WriteThenReadTest()
        {
            this.WriteProcess();
            this.ReadProcess();
        }
        
        [TestMethod]
        public void WriteWhenBufferIsFullTest()
        {
            // 확장 메서드를 이용하여 더미 데이터로 가득 채움
            this.MadWriter.WriteEmpty(MaximumSizeHigh);
            this.MadWriter.Flush();

            // 빈 데이터로 채웠으므로 이땐 TryWrite가 false가 되며, Write가 예약된다.
            this.WriteProcess();

            this.MadReader.TryRead(out var buffer, MaximumSizeHigh);
            // Advance되며 예약된 시그널(쓰기 프로세스 다시 발동) Set됨
            this.MadReader.AdvanceTo(buffer.Buffer.End);

            this.ReadProcess();

            // 처음 WriteProcess 호출되며 한번, 예약된 WriteProcess가 진행되며 두 번
            Assert.AreEqual(2, this.writeProcessPassed);
            Assert.AreEqual(1, this.readProcessPassed);
        }

        [TestMethod]
        public void PauseThresholdTest()
        {
            for (var i = 0; i < 100; ++i)
            {
                this.WriteProcess();
            }
            
            this.MadReader.TryRead(out var result, (int)this.Madline.Length);
            this.MadReader.AdvanceTo(result.Buffer.End);
            // Advance 과정에서 예약된 쓰기가 한 번 호출된다.

            // 예약된 쓰기작업 구문 분석(TryRead)
            this.ReadProcess();

            // 100번 WriteProcess 실행 + 예약된 쓰기시 WriteProcess 한 번 진입
            Assert.AreEqual(101, this.writeProcessPassed);
            Assert.AreEqual(1, this.readProcessPassed);

            // 이 이후로도 읽기 쓰기 잘되나 한번 확인용
            this.WriteProcess();
            this.ReadProcess();
        }

        [TestMethod]
        public void ReadWhenBufferIsEmptyTest()
        {
            // 읽을게 없음 : TryRead가 false로 되며 예약
            this.ReadProcess();
            this.ReadProcess();
            this.ReadProcess();
            // TryWrite의 Flush과정에서 읽기 예약된 게 있다면 실행
            this.WriteProcess();

            Assert.AreEqual(1, this.writeProcessPassed);
            // 처음 ReadProcess 2회 호출되며 세 번, 예약된 ReadProcess가 진행되며 네 번
            Assert.AreEqual(4, this.readProcessPassed);
        }
        
        [DataRow(24, 2)]
        [DataRow(36, 1)] // 그 이상 타겟 -> 들어와도 읽지 않음
        [TestMethod]
        public void TargetBytesTest(int targetLength, int readCalled)
        {
            this.WriteProcess();
            // 24를 타겟 : 24가 모일때까지는 읽기 중지
            this.ReadWithTargetBytesProcess(targetLength);
            // 이때 24가 모이면 읽기를 시작
            this.WriteProcess();

            Assert.AreEqual(2, this.writeProcessPassed);
            // 처음 ReadProcess 2회 호출되며 세 번, 예약된 ReadProcess가 진행되며 네 번
            Assert.AreEqual(readCalled, this.readProcessPassed);
        }
        public void ReadWithTargetBytesProcess(int targetLength)
        {
            ++this.readProcessPassed;
            if (this.MadReader.TryRead(out var result, targetLength))
            {
                Assert.AreNotEqual("Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual("Hello World!Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual(24, result.Buffer.Length);
                this.MadReader.AdvanceTo(result.Buffer.End);
            }
            else
            {
                this.MadReader.DoRead(out result, targetLength).Then(
                    readResult =>
                    {
                        this.ReadWithTargetBytesProcess(targetLength);
                    });
            }
        }
    }
}