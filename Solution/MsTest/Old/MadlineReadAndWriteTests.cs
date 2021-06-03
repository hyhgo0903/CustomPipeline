//using System.Buffers;
//using System.Text;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Tests.Infrastructure;

//namespace Tests
//{
//    [TestClass]
//    public sealed class MalineReadAndWriteTests : MadlineTest
//    {
//        [TestMethod]
//        public void ReadTest()
//        {
//            this.Madline.Writer.Write(new byte[] { 1 });
//            this.Madline.Writer.Write(new byte[] { 2 });
//            this.Madline.Writer.Write(new byte[] { 3 });
            
//            this.Madline.Writer.Flush();
            
//            this.Madline.Reader.TryRead(out var result);
//            byte[] data = result.ToArray();
//            // data는 {1, 2, 3} 이므로 다름
//            CollectionAssert.AreNotEqual(new byte[] { 1, 1, 1 }, data);
//            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, data);

//            // Advance 전 : 3
//            Assert.AreEqual(this.Madline.Length, 3);
//            this.Madline.Reader.Advance(result.End);
//            // Advance 후 : 0
//            Assert.AreEqual(this.Madline.Length, 0);
//        }
        
//        // 쓰고 읽는 과정
//        [TestMethod]
//        public void WriteTest()
//        {
//            var rawSource = new byte[] { 1, 2, 3 };
//            // 다 쓴 경우 비교하게 읽기
//            this.Madline.Writer.TryWrite(rawSource);
//            this.Madline.Writer.Flush();
//            this.Madline.Reader.TryRead(out var result);
//            byte[] data = result.ToArray();
//            // data는 {1, 2, 3} 이므로 다름
//            CollectionAssert.AreNotEqual(new byte[] { 1, 1, 1 }, data);
//            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, data);
            
//            this.Madline.Reader.Advance(result.End);
//        }
        
//        [TestMethod]
//        public void MultipleWriteTest()
//        {
//            var rawSource = new byte[] {1, 2};
//            this.Madline.Writer.TryWrite(rawSource);
//            rawSource = new byte[] { 3, 4, 5, 6 };
//            this.Madline.Writer.TryWrite(rawSource);
//            rawSource = new byte[] { 7, 8, 9 };
//            this.Madline.Writer.TryWrite(rawSource);
//            this.Madline.Reader.TryRead(out var result);
            
//            byte[] data = result.ToArray();

//            this.Madline.Reader.Advance(result.End);

//            CollectionAssert.AreNotEqual(new byte[] { 1, 1, 1, 1, 1, 1 }, data);
//            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, data);
//        }


//        [TestMethod]
//        public void MultipleWriteButNotFlushedTest()
//        {
//            var rawSource = new byte[] { 1, 2 };
//            this.Madline.Writer.TryWrite(rawSource);
//            rawSource = new byte[] { 3, 4, 5, 6 };
//            this.Madline.Writer.TryWrite(rawSource);

//            rawSource = new byte[] { 7, 8, 9 };
//            this.Madline.Writer.TryWrite(rawSource);
//            this.Madline.Reader.TryRead(out var result,
//                (rst) =>
//                {
//                    var rstArray = rst.ToArray();

//                    this.Madline.Reader.Advance(rst.End);

//                    CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, rstArray);
//                    CollectionAssert.AreNotEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, rstArray);
//                });
//            var data = result.ToArray();
//            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, data);
//        }

//        [TestMethod]
//        public void CompleteAfterAdvanceCommits()
//        {
//            this.Madline.Writer.WriteEmpty(4);

//            this.Madline.Writer.Flush();

//            this.Madline.Reader.TryRead(out var result);
//            Assert.AreEqual(4, result.Length);
//            this.Madline.Reader.Advance(result.End);
//        }

//    }
//}
