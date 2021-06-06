using System;
using MadPipeline.MadngineSource;

namespace MadPipeline
{
    public sealed class MadlineCallbacks
    {
        public MadlineCallbacks()
        {
            this.ReadPromise = new Promise<ReadResult>();
            this.WriteSignal = new Signal();
        }
        public Signal WriteSignal { get; }
        public Promise<ReadResult> ReadPromise { get; set; }
    }
}
