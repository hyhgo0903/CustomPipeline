using System;
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
            this.Madline.Callback.WriteSignal.OnCompleted(() => signalTest = 1);
            Assert.AreEqual(0, signalTest);
            this.Madline.Callback.WriteSignal.Set();
            Assert.AreEqual(1, signalTest);

            // 여러번 예약 달아도 Set전에 한 번만 예약된다.
            this.Madline.Callback.WriteSignal.OnCompleted(() => signalTest = 2);
            this.Madline.Callback.WriteSignal.OnCompleted(() => signalTest = 3);
            this.Madline.Callback.WriteSignal.OnCompleted(() => signalTest = 4);
            this.Madline.DoRead(out _);
            Assert.AreEqual(4, signalTest);
            this.Madline.DoWrite(new ReadOnlyMemory<byte>()).OnCompleted(() => signalTest = 5);
            this.Madline.DoRead(out _);
            Assert.AreEqual(5, signalTest);

        }

        [TestMethod]
        public void PromiseTest()
        {
            var promiseTest = 0;
            this.Madline.Callback.GetReadFuture().Then(_ => promiseTest = 1);
            Assert.AreEqual(0, promiseTest);
            this.Madline.Callback.ReadComplete(new ReadResult());
            Assert.AreEqual(1, promiseTest);

            //// 여러번 예약 달아도 Set전에 한 번만 예약된다.
            //this.Madline.Callback.WriteSignal.OnCompleted(() => promiseTest = 2);
            //this.Madline.Callback.WriteSignal.OnCompleted(() => promiseTest = 3);
            //this.Madline.DoRead(out _);
            //Assert.AreEqual(2, promiseTest);
            //this.Madline.DoWrite(new ReadOnlyMemory<byte>()).OnCompleted(() => promiseTest = 4);
            //this.Madline.DoRead(out _);
            //Assert.AreEqual(4, promiseTest);

        }
    }
}
