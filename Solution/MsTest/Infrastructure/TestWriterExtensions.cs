namespace Tests.Infrastructure
{
    using MadPipeline;

    public static class TestWriterExtensions
    {
        public static MadlineWriter WriteEmpty(this MadlineWriter writer, int count)
        {
            //writer.GetSpan(count).Slice(0, count).Clear();
            writer.GetSpan(count)[..count].Clear();
            writer.Advance(count);
            return writer;
        }
    }
}
