using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Infrastructure;

namespace Tests
{
    [TestClass]
    public sealed class MadlineCallbackTests : MadlineTest
    {

        // 읽기를 먼저 하고 콜백에 등록하여 쓴 이후에 비로소(?) 발동되는 과정
        [TestMethod]
        public void ReadThenWriteTest()
        {
            // 이땐 콜백을 큐에 등록해야함
            // 함수가 실행된 경우는 true, 안되고 큐에 등록된 경우는 false이므로 함수가 false를 반환해야한다.

            var isRead = this.Madline.Reader.ReadAsync(out var result,
                (rst) =>
                {
                    Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(rst.ToArray()));
                    Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(rst.ToArray()));
                    this.Madline.Reader.AdvanceTo(rst.End);
                });
            Assert.IsFalse(isRead);
            // 여러번 읽기 예약해도 하나만
            Assert.IsFalse(this.Madline.Reader.ReadAsync(out var anotherResult));
            Assert.AreEqual(0, result.Length);
            Assert.AreEqual(0, anotherResult.Length);

            var rawSource = Encoding.ASCII.GetBytes("Hello World!");
            // 읽기 작업 끝나며 앞서 등록된 ReadAsync 작업 재실행됨
            this.Madline.Writer.WriteAsync(rawSource);
            this.Madline.Writer.Flush();
            this.Madline.Writer.Complete();
        }


        [TestMethod]
        // 버퍼 꽉 찼을 때 입력하면 큐로 넣어야
        public void WriteTestWhenBufferIsFull()
        {
            // PauseWriterThreshold이상으로 쓰기를 한 경우
            this.Madline.Writer.WriteEmpty(MaximumSizeHigh);
            this.Madline.Writer.Flush();

            var rawSource = new byte[] { 1, 2, 3 };

            // 이건 예약되기만 해야함(false)
            var isWritten = this.Madline.Writer.WriteAsync(rawSource, () =>
            {
                this.Madline.Writer.Flush();
                this.Madline.Reader.ReadAsync(out var result,
                    (rst) =>
                    {
                        var data = rst.ToArray();
                        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, data);
                        this.Madline.Reader.AdvanceTo(rst.End);
                    });
            });
            Assert.IsFalse(isWritten);

            this.Madline.Reader.ReadAsync(out var result2);
            // AdvanceTo 이후 예약된 WriteAsync가 재실행
            this.Madline.Reader.AdvanceTo(result2.End);
        }
    }

}
