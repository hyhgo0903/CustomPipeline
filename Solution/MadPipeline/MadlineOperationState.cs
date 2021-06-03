using System;

namespace MadPipeline
{
    internal struct MadlineOperationState
    {
        private State state;

        [Flags]
        internal enum State : byte
        {
            Reading = 0x01,
            Writing = 0x02,
            WritingPaused = 0x04,
            ReadingReserved = 0x08,
            WritingReserved = 0x10
        }
        public bool IsReadingActive => (this.state & State.Reading) == State.Reading;
        public bool IsWritingActive => (this.state & State.Writing) == State.Writing;
        public bool IsWritingPaused => (this.state & State.WritingPaused) == State.WritingPaused;
        public bool IsReadingReserved => (this.state & State.ReadingReserved) == State.ReadingReserved;
        public bool IsWritingReserved => (this.state & State.WritingReserved) == State.WritingReserved;
        
        public void BeginRead()
        {
            this.state |= State.Reading;
        }
        public void EndRead()
        {
            this.state &= ~State.Reading;
        }
        public void BeginWrite()
        {
            this.state |= State.Writing;
        }
        public void EndWrite()
        {
            this.state &= ~State.Writing;
        }
        public void PauseWrite()
        {
            this.EndWrite();
            this.state |= State.WritingPaused;
        }
        public void ResumeWrite()
        {
            this.BeginWrite();
            this.state &= ~State.WritingPaused;
        }
        public void ReserveRead()
        {
            this.state |= State.ReadingReserved;
        }
        public void EndReserveRead()
        {
            this.state &= ~State.ReadingReserved;
        }
        public void ReserveWrite()
        {
            this.state |= State.WritingReserved;
        }
        public void EndReserveWrite()
        {
            this.state &= ~State.WritingReserved;
        }

    }
}