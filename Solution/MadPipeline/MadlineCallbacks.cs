using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadPipeline.MadngineSource;

namespace MadPipeline
{
    public sealed class MadlineCallbacks
    {
        private readonly Promise<ReadResult> readPromise;

        public MadlineCallbacks()
        {
            this.readPromise = new Promise<ReadResult>();
            this.WriteSignal = new Signal();
        }
        public Signal WriteSignal { get; }

        public void Complete(ReadResult result) => this.readPromise.Complete(result);
        public void SetException(Exception exception) => this.readPromise.SetException(exception);
        public Future<ReadResult> GetFuture() => this.readPromise.GetFuture();

        public void OnCompleted(Action continuation) => this.WriteSignal.OnCompleted(continuation);
        public void Reset() => this.WriteSignal.Reset();
        public void Set() => this.WriteSignal.Set();
    }
}
