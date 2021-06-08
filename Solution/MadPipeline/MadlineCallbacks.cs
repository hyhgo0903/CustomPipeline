namespace MadPipeline
{
    using MadPipeline.MadngineSource;

    public sealed class MadlineCallbacks
    {
        public MadlineCallbacks()
        {
            this.ReadPromise = new Promise<ReadResult>();
            this.WriteSignal = new Signal();
        }
        
        public Promise<ReadResult> ReadPromise { get; set; }
        public Signal WriteSignal { get; }
    }
}
