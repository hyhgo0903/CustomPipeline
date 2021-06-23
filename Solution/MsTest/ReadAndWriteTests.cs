//namespace Tests
//{
//    using System.Text;
//    using Microsoft.VisualStudio.TestTools.UnitTesting;
//    using System.Buffers;

//    [TestClass]
//    public sealed class ReadAndWriteTests : MadlineTest
//    {

//        [TestMethod]
//        public void WriteReadTest()
//        {
//            // 헤더포함 12바이트
//            var readOnlyMemory = CreateMessageWithRandomBody(10);
//            this.SmallMadWriter.TryWrite(in readOnlyMemory);

//            this.SmallMadWriter.Flush();

//            this.SmallMadReader.TryRead(out var result);
//            var data = result.ToArray();

//            // 헤더를 검사해보고
//            // 헤더+바디길이 12맞는지 확인
//            Assert.AreEqual(12, result.Length);
//            // 바디길이 맞는지 확인
//            Assert.AreEqual(10, GetBodyLengthFromMessage(result));

//            // Advance 전 : 12
//            Assert.AreEqual(this.SmallMadline.Length, 12);
//            this.SmallMadReader.AdvanceTo(result.End);
//            // Advance 후 : 0
//            Assert.AreEqual(this.SmallMadline.Length, 0);
//        }

//        // 쓰고 읽는 과정
//        [TestMethod]
//        public void WriteWithMessageTest()
//        {
//            var rawSource = CreateMessage(new byte[] {1, 2, 3});
//            // 다 쓴 경우 비교하게 읽기
//            this.SmallMadWriter.TryWrite(rawSource);
//            this.SmallMadReader.TryRead(out var result);
//            Assert.AreEqual(3, GetBodyLengthFromMessage(result));
//            var data = result.ToArray();
//            CollectionAssert.AreEqual(new byte[] {3<<2, 0, 1, 2, 3}, data);
//            this.SmallMadReader.AdvanceTo(result.End);
//        }
        
//        [TestMethod]
//        public void NotReadWhenNotAllMessagesAreCome()
//        {
//            var rawSource = CreateMessage(new byte[] { 1, 2, 3 });
//            rawSource = rawSource.Slice(0, rawSource.Length - 1);
//            // 일부만 들어왔고 이 경우 읽히면 안 됨
//            this.SmallMadWriter.TryWrite(rawSource);

//            var expectZero = this.SmallMadReader.TryRead(out var result);
//            Assert.AreEqual(0, expectZero);
//            this.SmallMadReader.AdvanceTo(result.End);
//        }

//        [TestMethod]
//        public void MultipleWriteTest()
//        {
//            var rawSource = CreateMessage(Encoding.ASCII.GetBytes("Hello World!"));
//            // 다 쓴 경우 비교하게 읽기
//            this.SmallMadWriter.TryWrite(rawSource);
//            this.SmallMadWriter.TryWrite(rawSource);
//            this.SmallMadWriter.TryWrite(rawSource);
//            this.SmallMadReader.TryRead(out var result);
//            Assert.AreEqual(12, GetBodyLengthFromMessage(result));
//            var message = GetBodyFromMessage(result);
//            Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(message));
//            Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(message));
//            this.SmallMadReader.AdvanceTo(result.End);

//            this.SmallMadReader.TryRead(out result);
//            Assert.AreEqual(12, GetBodyLengthFromMessage(result));
//            Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(message));
//            this.SmallMadReader.AdvanceTo(result.End);

//            this.SmallMadReader.TryRead(out result);
//            Assert.AreEqual(12, GetBodyLengthFromMessage(result));
//            Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(message));
//            this.SmallMadReader.AdvanceTo(result.End);
//        }

//    }
//}