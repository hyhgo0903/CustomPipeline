using MadPipeline;

namespace Tests.Infrastructure
{
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
