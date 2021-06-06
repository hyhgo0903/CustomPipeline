using System;
using System.Text;
using MadPipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MadPipeline.MadngineSource;

namespace Tests
{
    [TestClass]
    public sealed class CallbackTest : MadlineTest
    {
        [TestMethod]
        public void SignalTest()
        {            
            var signalTest = 0;
            var signalTest2 = 0;

            // 시그널은 리셋기능으로 그때그때 만들지 않고
            // 한 개로 재사용 가능하다 -> Madline이 소유하는 구조로
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

            //var rawSource = Encoding.ASCII.GetBytes("Hello World!");
            //this.Madline.DoWrite().OnCompleted(() => signalTest = 5);
            //this.Madline.TryRead(out _);
            //Assert.AreEqual(5, signalTest);

        }

        [TestMethod]
        public void PromiseThenCompleteTest()
        {
            // promise는 그때그때 만들어져야
            // SetResult후 반환하는 구조로 한 뒤
            // Then으로 
            var promise = new Promise<ReadResult>();
            var promiseTest = 0;
            promise.GetFuture().Then(_ => promiseTest += 1);
            Assert.AreEqual(0, promiseTest);
            promise.Complete(new ReadResult());
            Assert.AreEqual(1, promiseTest);
            promise.Complete(new ReadResult());
            Assert.AreEqual(1, promiseTest);
        }
        [TestMethod]
        public void PromiseMultipleThenTest()
        {
            // Then으로 여러개 달기
            var promise = new Promise<ReadResult>();
            var promiseTest = 0;
            var promiseTest2 = 0;
            promise.GetFuture().Then(_ => promiseTest += 1)
                .Then(_ => promiseTest2 += 1);
            Assert.AreEqual(0, promiseTest2);
            Assert.AreEqual(0, promiseTest);
            promise.Complete(new ReadResult());
            Assert.AreEqual(1, promiseTest2);
            Assert.AreEqual(1, promiseTest);
        }
    }
}
