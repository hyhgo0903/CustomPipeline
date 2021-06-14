namespace MadPipeline.Thread
{
    using System.Threading;

    internal sealed class SyncEvents
    {
        internal EventWaitHandle ExitThreadEvent { get; }

        internal EventWaitHandle NewItemEvent { get; }

        internal WaitHandle[] EventArray { get; }

        internal SyncEvents()
        {
            NewItemEvent = new ManualResetEvent(false);
            ExitThreadEvent = new ManualResetEvent(false);
            EventArray = new WaitHandle[2];
            EventArray[0] = NewItemEvent;
            EventArray[1] = ExitThreadEvent;
        }
    }
}
