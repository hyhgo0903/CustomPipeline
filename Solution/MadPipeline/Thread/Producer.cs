namespace MadPipeline.Thread
{
    using System.Threading;

    internal sealed class Producer
    {
        private readonly SyncEvents syncEvents;
        private readonly MadlineOperationState operationState;
        internal Thread? Thread1 { get; set; }
        
        internal Producer(SyncEvents syncEvents, MadlineOperationState operationState)
        {
            this.syncEvents = syncEvents;
            this.operationState = operationState;
        }

    }
}
