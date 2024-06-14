namespace Praktika
{
    public class LogLineOptions
    {
        public string Pattern { get; set; } = @"^((?:.?\d{1,3}){4,6}) (.+) \[(.+)](.+)$";
        public LogLineReplacementOptions Replacement { get; set; } = new LogLineReplacementOptions();
    }
}
