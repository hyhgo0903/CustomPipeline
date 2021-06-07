using System;
using MadPipeline.MadngineSource;

namespace MadPipeline
{
    public sealed class MadlineSignals
    {
        public MadlineSignals()
        {
            this.ReadSignal = new Signal();
            this.WriteSignal = new Signal();
        }

        public Signal ReadSignal { get; }
        public Signal WriteSignal { get; }
    }
}
