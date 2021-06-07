namespace Tests
{
    using MadPipeline;
    using MadPipeline.MadngineSource;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public sealed class CallbackTest : MadlineTest
    {
        [TestMethod]
        public void SignalTest()
        {
            var signal = new Signal();
            var signalTest = 0;
            var signalTest2 = 0;

            // 시그널은 리셋기능으로 그때그때 만들지 않고
            // 한 개로 재사용 가능하다 -> Madline이 소유하는 구조로
            signal.OnCompleted(() => signalTest += 1);
            Assert.AreEqual(0, signalTest);
            signal.Set();
            Assert.AreEqual(1, signalTest);
            signal.Set();
            signal.Reset();
            
            Assert.AreEqual(1, signalTest);

            // 처음것만 실행
            signal.OnCompleted(() => signalTest2 += 1);
            signal.OnCompleted(() => signalTest += 1);
            signal.OnCompleted(() => signalTest += 1);
            signal.OnCompleted(() => signalTest += 1);
            Assert.AreEqual(0, signalTest2);
            signal.Set();
            signal.Reset();
            Assert.AreEqual(1, signalTest2);
            Assert.AreEqual(1, signalTest);

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
