namespace MadPipeline.Thread
{
    using System.Threading;

    internal sealed class Producer
    {
        private readonly SyncEvents syncEvents;
        private readonly MadlineOperationState operationState;
        internal Thread? ProducerThread { get; set; }
        
        internal Producer(SyncEvents syncEvents, MadlineOperationState operationState)
        {
            this.syncEvents = syncEvents;
            this.operationState = operationState;
        }

        internal bool SetProducerThread()
        {
            if (this.operationState.IsWritingActive)
            {
                return false;
            }
            this.ProducerThread = Thread.CurrentThread;
            return true;
        }


    }
}
