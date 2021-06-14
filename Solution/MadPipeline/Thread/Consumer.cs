namespace MadPipeline.Thread
{
    using System.Threading;

    internal sealed class Consumer
    {
        private readonly SyncEvents syncEvents;
        private readonly MadlineOperationState operationState;
        internal Thread? ConsumerThread { get; set; }

        internal Consumer(SyncEvents syncEvents, MadlineOperationState operationState)
        {
            this.syncEvents = syncEvents;
            this.operationState = operationState;
        }

        internal bool SetConsumerThread()
        {
            if (this.operationState.IsReadingActive)
            {
                return false;
            }
            this.ConsumerThread = Thread.CurrentThread;
            return true;
        }
        

    }
}
