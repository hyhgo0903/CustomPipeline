using System;
using System.Buffers;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public sealed class MadlineCallbackTests : MadlineTest
    {
        [TestMethod]
        public void ReadThenWriteTest()
        {
            WriteProcess();
            var isRead = this.Madline.Reader.TryRead(out var result);
            Assert.IsTrue(isRead);
            Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
            Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
            Assert.AreEqual(12, result.Buffer.Length);
        }
        public void WriteProcess()
        {
            var received = Encoding.ASCII.GetBytes("Hello World!");
            if (this.Madline.TryWrite(received) == false)
            {
                this.Madline.Callback.WriteSignal.OnCompleted(
                    () =>
                    {
                        this.WriteProcess();
                    });
            }
        }

        //public void ReadProcess()
        //{
        //    if (this.Madline.TryRead(out var result, size))
        //    {
        //        this.SendToSocket(result.Buffer);
        //    }
        //    else
        //    {
        //        this.Madline.DoRead(size)
        //            .Then(result =>
        //            {
        //                this.SendToSocket(result.Buffer);
        //            });
        //    }
        //}

    }
}