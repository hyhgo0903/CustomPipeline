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
    }
}
