namespace Praktika
{
    public class LogLineReplacementOptions
    {
        public string IpAdress { get; set; } = "$1";
        public string DateTime { get; set; } = "$3";
        public LogLineReplacementDateTimeOptions DateTimeReplacement { get; set; } = new LogLineReplacementDateTimeOptions();
        public string Body { get; set; } = "$2$4";
    }
}
