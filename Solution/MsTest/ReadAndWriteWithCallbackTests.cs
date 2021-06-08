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
                this.MadWriter.DoWrite().OnCompleted(
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
            if (this.MadReader.TryRead(out var result))
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
                this.MadReader.DoRead().Then(
                    readResult =>
                    {
                        Assert.AreEqual(12, readResult.Buffer.Length);
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
        public void MultipleWriteThenReadTest()
        {
            this.WriteProcess();
            this.WriteProcess();
            this.ReadTwiceWrittenProcess();
        }
        public void ReadTwiceWrittenProcess()
        {
            ++this.readProcessPassed;
            if (this.MadReader.TryRead(out var result))
            {
                Assert.AreNotEqual("Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual("Hello World!Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual(24, result.Buffer.Length);
                this.MadReader.AdvanceTo(result.Buffer.End);
            }
            else
            {
                this.MadReader.DoRead().Then(
                    readResult =>
                    {
                        Assert.AreEqual(24, readResult.Buffer.Length);
                        this.ReadTwiceWrittenProcess();
                    });
            }
        }

        [TestMethod]
        public void WriteWhenBufferIsFullTest()
        {
            // 확장 메서드를 이용하여 더미 데이터로 가득 채움
            this.MadWriter.WriteEmpty(MaximumSizeHigh);
            this.MadWriter.Flush();

            // 빈 데이터로 채웠으므로 이땐 TryWrite가 false가 되며, Write가 예약된다.
            this.WriteProcess();

            this.MadReader.TryRead(out var buffer);
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
            
            this.MadReader.TryRead(out var result);

            // PauseThreshold를 넘으면 더 이상 받을 수 없음
            // 이것을 넘어가는 쓰기는 예약이 된다. (하나까지만)
            Assert.IsTrue(result.Buffer.Length <= MaximumSizeHigh + 12);

            // Advance 과정에서 예약된 쓰기가 한 번 호출된다.
            this.MadReader.AdvanceTo(result.Buffer.End);
            // 예약된 쓰기작업 구문 분석(TryRead)
            this.ReadProcess();

            Assert.AreEqual(101, this.writeProcessPassed);
            Assert.AreEqual(1, this.readProcessPassed);

            // 이 이후로도 읽기 쓰기 잘되나 한번 확인용
            this.WriteProcess();
            this.ReadProcess();
            Assert.AreEqual(102, this.writeProcessPassed);
            Assert.AreEqual(2, this.readProcessPassed);
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
        


        [TestMethod]
        public void TargetBytesTest()
        {
            // 읽을게 없음 : TryRead가 false로 되며 예약
            this.ReadTwiceWrittenProcess();
            // 소스의 길이가 타겟바이트보다 적음(더 들어와야 예약이 실행됨 -> 여전히 예약)
            this.WriteProcessWithTargetBytes();

            Assert.AreEqual(1, this.writeProcessPassed);
            // 앞선 테스트와 달리 예약된 읽기 작업이 실행되지 않았음 -> 1번 호출
            Assert.AreEqual(1, this.readProcessPassed);

            // 더 들어와서 기록과 함께 예약이 실행
            this.WriteProcessWithTargetBytes();

            Assert.AreEqual(2, this.writeProcessPassed);
            Assert.AreEqual(2, this.readProcessPassed);
        }
        public void WriteProcessWithTargetBytes()
        {
            ++this.writeProcessPassed;
            var received = Encoding.ASCII.GetBytes("Hello World!");
            // 타겟바이트가 기록되는 데이터보다 크도록 설정
            if (this.MadWriter.TryWrite(received, 20) == false)
            {
                this.MadWriter.DoWrite().OnCompleted(
                    () =>
                    {
                        this.WriteProcessWithTargetBytes();
                    });
            }
        }

        [TestMethod]
        public void ReadWithTargetLength()
        {
            this.WriteProcess();

            // 6 길이만 읽게끔
            this.MadReader.TryRead(out var result, 6);
            Assert.AreNotEqual("Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
            Assert.AreEqual("Hello ", Encoding.ASCII.GetString(result.Buffer.ToArray()));
            this.MadReader.AdvanceTo(result.Buffer.End);

            this.WriteProcess();

            this.MadReader.TryRead(out var result2, 10);
            Assert.AreEqual("World!Hell", Encoding.ASCII.GetString(result2.Buffer.ToArray()));
            this.MadReader.AdvanceTo(result2.Buffer.End);

            this.MadReader.TryRead(out var result3);
            Assert.AreEqual("o World!", Encoding.ASCII.GetString(result3.Buffer.ToArray()));
            this.MadReader.AdvanceTo(result3.Buffer.End);
        }

    }
}