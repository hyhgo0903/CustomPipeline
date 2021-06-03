using System.Buffers;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Infrastructure;

namespace Tests
{
    [TestClass]
    public sealed class MadlineCallbackTests : MadlineTest
    {
        [TestMethod]
        public void ReadThenWriteTest()
        {
            var rawSource = Encoding.ASCII.GetBytes("Hello World!");
            // 읽기 작업 끝나며 앞서 등록된 TryRead 작업 재실행됨
            this.Madline.Writer.TryWrite(rawSource);

            var isRead = this.Madline.Reader.TryRead(out var result);
            Assert.IsTrue(isRead);
            Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
            Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
            this.Madline.Reader.Advance(result.Buffer.End);

            // 여러번 읽기 예약해도 하나만
            Assert.IsFalse(this.Madline.Reader.TryRead(out var anotherResult));
            Assert.AreEqual(12, result.Buffer.Length);
            
        }
    }
}