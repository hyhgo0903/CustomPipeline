namespace MadPipeline
{
    using MadngineSource;

    public sealed class MadlineCallbacks
    {
        public MadlineCallbacks()
        {
            this.ReadPromise = new Promise<ReadResult>();
            this.WriteSignal = new Signal();
        }
        
        public Promise<ReadResult> ReadPromise { get; set; }
        public Signal WriteSignal { get; }

        // Promise 꼬이는거 방지해서 일단 마련 (signal은 프로퍼티 불러서 수동으로 Reset호출)
        public Promise<ReadResult> ResetReadPromise()
        {
            this.ReadPromise = new Promise<ReadResult>();
            return this.ReadPromise;
        }
    }
}
