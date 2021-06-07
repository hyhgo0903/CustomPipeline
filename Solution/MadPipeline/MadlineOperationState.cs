namespace MadPipeline
{
    using System;

    internal struct MadlineOperationState
    {
        private State state;

        [Flags]
        internal enum State : byte
        {
            Reading = 0x01,
            Writing = 0x02,
            WritingPaused = 0x04
        }
        public bool IsReadingActive => (this.state & State.Reading) == State.Reading;
        public bool IsWritingActive => (this.state & State.Writing) == State.Writing;
        public bool IsWritingPaused => (this.state & State.WritingPaused) == State.WritingPaused;
        
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

    }
}