using System.Globalization;
using System.Net;

namespace Praktika
{
    public class CfgOptions
    {
        public const string SectionName = "Cfg";

        public string LogFileName { get; set; } = "access.log";

        public PostgresDbSqlOptions PostgresDb { get; set; } = new PostgresDbSqlOptions();

        public LogLineOptions LogLine { get; set; } = new LogLineOptions();

        public bool Help { get; set; } = false;
        public bool Service { get; set; } = false;

        public bool Parse { get; set; } = false;
        public bool Print { get; set; } = false;

        public bool ByDate { get; set; } = true;

        public string FilterIp { get; set; } = null;

        public IPAddress GetFilterIp()
        {
            return FilterIp == null ? null : IPAddress.Parse(FilterIp);
        }

        public string FilterDate { get; set; }

        public DateTime? GetFilterDate()
        {
            return ParseFilterDate(FilterDate);
        }

        public string FilterDate1 { get; set; }
        public string FilterDate2 { get; set; }

        public DateTime? GetFilterDate1()
        {
            return ParseFilterDate(FilterDate1);
        }
        public DateTime? GetFilterDate2()
        {
            return ParseFilterDate(FilterDate2);
        }

        public DateTime? ParseFilterDate(string str)
        {
            if (str == null) return null;

            DateTime dateTime;

            var culture = CultureInfo.InvariantCulture;

            var flags = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;

            dateTime = DateTime.ParseExact(str, "dd.MM.yyyy", culture, flags);

            return dateTime;
        }

        public void Validate()
        {
            IPAddress filterIp = GetFilterIp();

            DateTime? filterDate = GetFilterDate();
            
            DateTime? filterDate1 = GetFilterDate1();
            DateTime? filterDate2 = GetFilterDate2();

            if(filterDate != null && filterDate1 != null)
            {
                throw new Exception("Cfg:FilterDate1 and Cfg:FilterDate are mutually exclusive.");
            }

            if(filterDate2 != null && filterDate1 == null)
            {
                throw new Exception("Specify Cfg:FilterDate1.");
            }

            if (filterDate1 != null && filterDate2 == null)
            {
                throw new Exception("Specify Cfg:FilterDate2.");
            }

            if (filterDate2 < filterDate1)
            {
                throw new Exception("Cfg:FilterDate2 < Cfg:FilterDate1.");
            }
        }

        public void Coerce()
        {
            if(!Parse && !Print)
            {
                Parse = true;
                Print = true;
            }

            if(GetFilterIp() != null || GetFilterDate() != null || GetFilterDate1() != null)
            {
                Print = true;
            }
        }
    }
}
