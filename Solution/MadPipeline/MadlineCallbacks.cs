using System.Threading;

namespace MadPipeline
{
    using MadngineSource;

    public sealed class MadlineCallbacks
    {
        private Promise<ReadResult> readPromise;
        private readonly Signal writeSignal;

        public MadlineCallbacks()
        {
            this.ReadPromise = new Promise<ReadResult>();
            this.writeSignal = new Signal();
        }

        public Promise<ReadResult> ReadPromise
        {
            get => readPromise;
            set => readPromise = value;
        }

        public Signal WriteSignal => writeSignal;

        // Promise 꼬이는거 방지해서 일단 마련 (signal은 프로퍼티 불러서 수동으로 Reset호출)
        public Promise<ReadResult> NewReadPromise()
        {
            this.ReadPromise = new Promise<ReadResult>();
            return this.ReadPromise;
        }

        public void ResetReadPromise()
        {
            var promise = new Promise<ReadResult>();
            Interlocked.Exchange(ref this.readPromise, promise);
        }
    }
}
