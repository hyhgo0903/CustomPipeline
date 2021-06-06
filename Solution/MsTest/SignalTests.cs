using System;
using System.Text;
using MadPipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public sealed class SignalTests : MadlineTest
    {
        [TestMethod]
        public void SignalTest()
        {
            var signalTest = 0;
            var signalTest2 = 0;
            this.Madline.Callback.WriteSignal.OnCompleted(() => signalTest += 1);
            Assert.AreEqual(0, signalTest);
            this.Madline.Callback.WriteSignal.Set();
            Assert.AreEqual(1, signalTest);
            this.Madline.Callback.WriteSignal.Set();
            this.Madline.Callback.WriteSignal.Reset();
            Assert.AreEqual(1, signalTest);

            // 처음것만 실행
            this.Madline.Callback.WriteSignal.OnCompleted(() => signalTest2 += 1);
            this.Madline.Callback.WriteSignal.OnCompleted(() => signalTest += 1);
            this.Madline.Callback.WriteSignal.OnCompleted(() => signalTest += 1);
            this.Madline.Callback.WriteSignal.OnCompleted(() => signalTest += 1);
            Assert.AreEqual(0, signalTest2);
            this.Madline.Callback.WriteSignal.Set();
            this.Madline.Callback.WriteSignal.Reset();
            Assert.AreEqual(1, signalTest2);
            Assert.AreEqual(1, signalTest);

            var rawSource = Encoding.ASCII.GetBytes("Hello World!");
            this.Madline.DoWrite(rawSource).OnCompleted(() => signalTest = 5);
            this.Madline.DoRead(out _);
            Assert.AreEqual(5, signalTest);

        }

        [TestMethod]
        public void PromiseTest()
        {
            var promiseTest = 0;
            var promiseTest2 = 0;
            this.Madline.Callback.GetReadFuture().Then(_ => promiseTest += 1);
            Assert.AreEqual(0, promiseTest);
            this.Madline.Callback.ReadComplete(new ReadResult());
            Assert.AreEqual(1, promiseTest);

            // 여러번 하면 순서대로 실행?
            this.Madline.Callback.GetReadFuture().Then(_ => promiseTest2 += 1)
                .Then(_ => promiseTest += 1);
            Assert.AreEqual(1, promiseTest2);
            Assert.AreEqual(2, promiseTest);
            this.Madline.Callback.ReadComplete(new ReadResult());
            Assert.AreEqual(2, promiseTest2);
            Assert.AreEqual(3, promiseTest);

            ReadResult result;
            this.Madline.DoRead(out result).Then(result => promiseTest = 4);

            //this.Madline.DoWrite(new ReadOnlyMemory<byte>()).OnCompleted(() => promiseTest = 4);
            //this.Madline.DoRead(out _);
            //Assert.AreEqual(4, promiseTest);

        }
    }
}
