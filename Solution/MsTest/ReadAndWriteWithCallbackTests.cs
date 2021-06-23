namespace Tests
{
    using System.Buffers;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class ReadAndWriteWithCallbackTests : MadlineTest
    {
        private int writeProcessPassed;
        private int readProcessPassed;

        // 데이터를 읽도록 시도하고, 실패한 경우 예약
        public void WriteProcess()
        {
            ++this.writeProcessPassed;
            var rawSource = Encoding.ASCII.GetBytes("Hello World!");
            if (this.SmallMadWriter.TryWrite(rawSource) == false)
            {
                // TryWrite에 실패한다면 이 함수를 액션으로 예약
                this.SmallMadWriter.WriteSignal().OnCompleted(
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
            if (this.SmallMadReader.TryRead(out var result, 0))
            {
                Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(result));
                Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(result));
                Assert.AreEqual(12, result.Length);
                this.SmallMadReader.AdvanceTo(result.End);
            }
            else
            {
                this.SmallMadReader.DoRead().Then(
                    readResult =>
                    {
                        ++this.readProcessPassed;
                        Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(readResult));
                        Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(readResult));
                        Assert.AreEqual(12, readResult.Length);
                        this.SmallMadReader.AdvanceTo(readResult.End);
                    });
            }
        }
        public void SendToSocket(in ReadOnlySequence<byte> result)
        {
            this.SmallMadReader.AdvanceTo(result.End);
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

            Assert.IsTrue(this.SmallMadline.State.IsWritingPaused);
            
            this.SmallMadReader.TryRead(out var result, 0);
            this.SmallMadReader.AdvanceTo(result.End);
            // Advance 과정에서 예약된 쓰기가 한 번 호출된다.

            // 예약된 쓰기작업 구문 분석(TryRead)
            this.ReadProcess();

            // 100번 WriteProcess 실행 + 예약된 쓰기시 WriteProcess 한 번 진입
            Assert.AreEqual(101, this.writeProcessPassed);
            Assert.IsFalse(this.SmallMadline.State.IsWritingPaused);
            // 이 이후로도 읽기 쓰기 잘되나 한번 확인용
            this.WriteProcess();
            this.ReadProcess();
        }

        [TestMethod]
        public void ReadWhenBufferIsEmptyTest()
        {
            // 읽을게 없음 : TryRead가 false로 되며 예약
            this.ReadProcess();
            // TryWrite의 Flush과정에서 읽기 예약된 게 있다면 실행
            this.WriteProcess();
            this.WriteProcess();
            this.WriteProcess();
            
            // 처음 ReadProcess 2회 호출되며 세 번, 예약된 ReadProcess가 진행되며 네 번
            Assert.AreEqual(2, this.readProcessPassed);
        }
        
    }
}