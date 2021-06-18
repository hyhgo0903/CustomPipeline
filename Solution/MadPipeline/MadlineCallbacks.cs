using System.Buffers;
using System.Threading;

namespace MadPipeline
{
    using MadngineSource;

    public sealed class MadlineCallbacks
    {
        private Promise<ReadOnlySequence<byte>> readPromise;

        public MadlineCallbacks()
        {
            this.readPromise = new Promise<ReadOnlySequence<byte>>();
            this.WriteSignal = new Signal();
            this.AdvanceSignal = new Signal();
        }

        public Promise<ReadOnlySequence<byte>> ReadPromise => readPromise;

        public Signal WriteSignal { get; }
        public Signal AdvanceSignal { get; }


        public void ResetReadPromise()
        {
            var promise = new Promise<ReadOnlySequence<byte>>();
            Interlocked.Exchange(ref this.readPromise, promise);
        }
    }
}
