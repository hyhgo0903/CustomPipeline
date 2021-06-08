namespace Tests.Infrastructure
{
    using MadPipeline;

    public static class TestWriterExtensions
    {
        public static IMadlineWriter WriteEmpty(this IMadlineWriter madline, int count)
        {
            madline.GetSpan(count)[..count].Clear();
            madline.Advance(count);
            return madline;
        }
    }
}
