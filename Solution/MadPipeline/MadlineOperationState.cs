namespace MadPipeline
{
    using System;

    public struct MadlineOperationState
    {
        private State state;

        [Flags]
        internal enum State : byte
        {
            Reading = 0x01,
            Writing = 0x02,
            WritingPaused = 0x04,
            ReadingPaused = 0x08
        }
        public bool IsReadingActive => (this.state & State.Reading) == State.Reading;
        public bool IsWritingActive => (this.state & State.Writing) == State.Writing;
        public bool IsWritingPaused => (this.state & State.WritingPaused) == State.WritingPaused;
        public bool IsReadingPaused => (this.state & State.ReadingPaused) == State.ReadingPaused;

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
            this.state |= State.WritingPaused;
        }
        public void ResumeWrite()
        {
            this.state &= ~State.WritingPaused;
        }
        public void PauseRead()
        {
            this.state |= State.ReadingPaused;
        }
        public void ResumeRead()
        {
            this.state &= ~State.ReadingPaused;
        }

    }
}