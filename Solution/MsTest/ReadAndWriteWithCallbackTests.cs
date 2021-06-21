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
            if (resultInt > 0)
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
                        ++this.readProcessPassed;
                        var message = GetBodyFromMessage(readResult);
                        Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(message));
                        Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(message));
                        Assert.AreEqual(12, GetBodyLengthFromMessage(readResult));
                        this.MadReader.AdvanceTo(readResult.End);
                        if (resultInt == 1)
                        {
                            // 아직 읽을 게 남은 경우이므로 다시 읽기 시도
                            this.ReadProcess();
                        }
                    });
            }
        }
        public void SendToSocket(in ReadOnlySequence<byte> result)
        {
            this.MadReader.AdvanceTo(result.End);
        }


        [TestMethod]
        public void WriteThenReadTest()
        {
            this.WriteProcess();
            this.ReadProcess();
        }

        [TestMethod]
        public void PauseThresholdTest()
        {
            for (var i = 0; i < 100; ++i)
            {
                this.WriteProcess();
            }

            Assert.IsTrue(this.Madline.State.IsWritingPaused);
            
            this.MadReader.TryRead(out var result);
            this.MadReader.AdvanceTo(result.End);
            // Advance 과정에서 예약된 쓰기가 한 번 호출된다.

            // 예약된 쓰기작업 구문 분석(TryRead)
            this.ReadProcess();

            // 100번 WriteProcess 실행 + 예약된 쓰기시 WriteProcess 한 번 진입
            Assert.AreEqual(101, this.writeProcessPassed);
            Assert.IsFalse(this.Madline.State.IsWritingPaused);
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
            this.WriteProcess();
            this.WriteProcess();
            
            // 처음 ReadProcess 2회 호출되며 세 번, 예약된 ReadProcess가 진행되며 네 번
            Assert.AreEqual(4, this.readProcessPassed);
        }


        [TestMethod]
        public void ReadCallOnlyOnceTests()
        {
            this.WriteProcess();
            this.WriteProcess();
            this.WriteProcess();
            // 타겟 알아서 잡으면서 끝까지 읽는지
            this.ReadProcess();

            Assert.AreEqual(3, this.writeProcessPassed);
            Assert.AreEqual(3, this.readProcessPassed);
        }
    }
}