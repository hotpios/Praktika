namespace Praktika
{
    public class LogLineReplacementDateTimeOptions
    {
        public string Pattern { get; set; } = @"(.+ [+-]\d{2})(\d{2})";
        public string Replacement { get; set; } = @"$1:$2";
        public string ParseFormat { get; set; } = @"dd/MMM/yyyy:HH:mm:ss zzz";
    }
}
