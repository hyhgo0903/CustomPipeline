namespace Tests
{
    using System.Buffers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Infrastructure;

    [TestClass]
    public sealed class ReadAndWriteTests : MadlineTest
    {
        [TestMethod]
        public void ReadTest()
        {
            this.MadWriter.Write(new byte[] {1});
            this.MadWriter.Write(new byte[] {2});
            this.MadWriter.Write(new byte[] {3});

            this.MadWriter.Flush();

            this.MadReader.TryRead(out var result, 0);
            var data = result.Buffer.ToArray();
            // data는 {1, 2, 3} 이므로 다름
            CollectionAssert.AreNotEqual(new byte[] {1, 1, 1}, data);
            CollectionAssert.AreEqual(new byte[] {1, 2, 3}, data);

            // Advance 전 : 3
            Assert.AreEqual(this.Madline.Length, 3);
            this.MadReader.AdvanceTo(result.Buffer.End);
            // Advance 후 : 0
            Assert.AreEqual(this.Madline.Length, 0);
        }

        // 쓰고 읽는 과정
        [TestMethod]
        public void WriteTest()
        {
            var rawSource = new byte[] {1, 2, 3};
            // 다 쓴 경우 비교하게 읽기
            this.MadWriter.TryWrite(rawSource);
            this.MadReader.TryRead(out var result, 0);
            var data = result.Buffer.ToArray();
            CollectionAssert.AreNotEqual(new byte[] {1, 1, 1}, data);
            CollectionAssert.AreEqual(new byte[] {1, 2, 3}, data);

            this.MadReader.AdvanceTo(result.Buffer.End);
        }

        [TestMethod]
        public void MultipleWriteTest()
        {
            var rawSource = new byte[] {1, 2};
            this.MadWriter.TryWrite(rawSource);
            rawSource = new byte[] {3, 4, 5, 6};
            this.MadWriter.TryWrite(rawSource);
            rawSource = new byte[] {7, 8, 9};
            this.MadWriter.TryWrite(rawSource);
            this.MadReader.TryRead(out var result, 0);

            byte[] data = result.Buffer.ToArray();

            this.MadReader.AdvanceTo(result.Buffer.End);

            CollectionAssert.AreNotEqual(new byte[] {1, 1, 1, 1, 1, 1}, data);
            CollectionAssert.AreEqual(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9}, data);
        }


        [TestMethod]
        public void MultipleWriteButNotFlushedTest()
        {
            this.MadWriter.Write(new byte[] { 1, 2 });
            this.MadWriter.Write(new byte[] { 3, 4, 5, 6 });
            this.MadWriter.Flush();
            // 이건 Flush되지 않음
            this.MadWriter.Write(new byte[] { 7, 8, 9 });
            this.MadReader.TryRead(out var result, 0);
            var data = result.Buffer.ToArray();
            CollectionAssert.AreNotEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, data);
            CollectionAssert.AreEqual(new byte[] {1, 2, 3, 4, 5, 6}, data);
        }

        [TestMethod]
        public void CompleteAfterAdvanceCommits()
        {
            this.MadWriter.WriteEmpty(4);

            this.MadWriter.Flush();
            this.MadWriter.CompleteWriter();

            this.MadReader.TryRead(out var result, 0);
            Assert.AreEqual(4, result.Buffer.Length);
            this.MadReader.AdvanceTo(result.Buffer.End);
            this.MadReader.CompleteReader();
        }
    }
}