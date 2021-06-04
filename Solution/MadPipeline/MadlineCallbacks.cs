using System;
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

        public void ReadComplete(ReadResult result) => this.readPromise.Complete(result);
        public void SetReadException(Exception exception) => this.readPromise.SetException(exception);
        public Future<ReadResult> GetReadFuture() => this.readPromise.GetFuture();

        public void OnWriteCompleted(Action continuation) => this.WriteSignal.OnCompleted(continuation);
        public void WriteReset() => this.WriteSignal.Reset();
        public void WriteSet() => this.WriteSignal.Set();
    }
}
