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
<<<<<<< HEAD
=======
<<<<<<< HEAD
=======
        [TestMethod]
        public void ReadTest()
        {
            this.Madline.Writer.Write(new byte[] { 1 });
            this.Madline.Writer.Write(new byte[] { 2 });
            this.Madline.Writer.Write(new byte[] { 3 });
            
            this.Madline.Writer.Flush();
            
            this.Madline.Reader.ReadAsync(out var result);
            byte[] data = result.ToArray();
            // data는 {1, 2, 3} 이므로 다름
            CollectionAssert.AreNotEqual(new byte[] { 1, 1, 1 }, data);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, data);

            // Advance 전 : 3
            Assert.AreEqual(this.Madline.Length, 3);
            this.Madline.Reader.AdvanceTo(result.End);
            // Advance 후 : 0
            Assert.AreEqual(this.Madline.Length, 0);
        }
        
        // 쓰고 읽는 과정
        [TestMethod]
        public void WriteTest()
        {
            var rawSource = new byte[] { 1, 2, 3 };
            // 다 쓴 경우 비교하게 읽기
            this.Madline.Writer.WriteAsync(rawSource);
            this.Madline.Writer.Flush();
            this.Madline.Reader.ReadAsync(out var result);
            byte[] data = result.ToArray();
            // data는 {1, 2, 3} 이므로 다름
            CollectionAssert.AreNotEqual(new byte[] { 1, 1, 1 }, data);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, data);
            
            this.Madline.Reader.AdvanceTo(result.End);
        }
        

        [TestMethod]
        public void MultipleWriteTest()
        {
            var rawSource = new byte[] {1, 2};
            this.Madline.Writer.WriteAsync(rawSource);
            rawSource = new byte[] { 3, 4, 5, 6 };
            this.Madline.Writer.WriteAsync(rawSource);
            rawSource = new byte[] { 7, 8, 9 };
            this.Madline.Writer.WriteAsync(rawSource,
                ()=>this.Madline.Writer.Flush());
            this.Madline.Reader.ReadAsync(out var result);
            
            byte[] data = result.ToArray();

            this.Madline.Reader.AdvanceTo(result.End);

            CollectionAssert.AreNotEqual(new byte[] { 1, 1, 1, 1, 1, 1 }, data);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, data);
        }

>>>>>>> parent of 4c458c2 (리뷰 전 푸시)
>>>>>>> main

        // 읽기를 먼저 하고 콜백에 등록하여 쓴 이후에 비로소(?) 발동되는 과정
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
<<<<<<< HEAD

            // 여러번 읽기 예약해도 하나만
            Assert.IsFalse(this.Madline.Reader.TryRead(out var anotherResult));
            Assert.AreEqual(12, result.Buffer.Length);

=======

            // 여러번 읽기 예약해도 하나만
            Assert.IsFalse(this.Madline.Reader.TryRead(out var anotherResult));
            Assert.AreEqual(12, result.Buffer.Length);

>>>>>>> main
        }
    }
}