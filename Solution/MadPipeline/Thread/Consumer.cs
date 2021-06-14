namespace MadPipeline.Thread
{
    using System.Threading;

    internal sealed class Consumer
    {
        private readonly SyncEvents syncEvents;
        private readonly MadlineOperationState operationState;
        internal Thread? Thread1 { get; set; }

        internal Consumer(SyncEvents syncEvents, MadlineOperationState operationState)
        {
            this.syncEvents = syncEvents;
            this.operationState = operationState;
        }
        
    }
}
