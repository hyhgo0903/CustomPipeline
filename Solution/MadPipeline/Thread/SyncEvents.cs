namespace MadPipeline.Thread
{
    using System.Threading;

    // 스레드 제어용. Producer Consumer가 공유함.
    internal sealed class SyncEvents
    {
        // 수동으로 Reset 시켜줘야. 여러 스레드가 응답
        internal EventWaitHandle ExitThreadEvent { get; }

        // AutoReset : 하나씩 응답(자동으로 리셋)
        internal EventWaitHandle NewItemEvent { get; }

        internal WaitHandle[] EventArray { get; }

        internal SyncEvents()
        {
            NewItemEvent = new AutoResetEvent(false);
            ExitThreadEvent = new ManualResetEvent(false);

            // WaitOne(0, false) : 시그널 받으면 true 아니면 false반환
            // while (!syncEvents.ExitThreadEvent.WaitOne(0, false)) 이런식으로 Producer 루프돌림 (ex) Exit의 신호가 오고있지 않은 경우)

            // 신호가 전달된 이벤트의 인덱스를 보여줌
            // while (WaitHandle.WaitAny(syncEvents.EventArray) != 1) 이런식으로 Consumer 사용(ex) 1이 켜지면 종료)

            // 배열 구성은 필요에 따라서
            EventArray = new WaitHandle[2];
            EventArray[0] = NewItemEvent;
            EventArray[1] = ExitThreadEvent;

            // 1Producer 1Consumer이니 AutoResetEvent만으로 되지 않을까..
        }
    }
}
