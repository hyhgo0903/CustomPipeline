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
            ReadProcess();
        }
        public void WriteProcess()
        {
            var received = Encoding.ASCII.GetBytes("Hello World!");
            if (this.Madline.TryWrite(received) == false)
            {
                this.Madline.DoWrite().OnCompleted(
                    () =>
                    {
                        this.WriteProcess();
                    });
            }
        }
        public void ReadProcess()
        {
            if (this.Madline.TryRead(out var result))
            {
                Assert.AreNotEqual("Hell World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual("Hello World!", Encoding.ASCII.GetString(result.Buffer.ToArray()));
                Assert.AreEqual(12, result.Buffer.Length);
            }
            else
            {
                this.Madline.DoRead().Then(
                    result =>
                    {
                        this.ReadProcess();
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