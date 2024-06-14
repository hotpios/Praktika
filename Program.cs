using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Npgsql;
using Microsoft.AspNetCore.Mvc;

namespace Praktika
{
    public static class Program
    {
        static CfgOptions _cfg;

        /// <summary>
        /// Главная (первая исполняемая) функция программы.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var app = builder.Build();

            _cfg = app.Configuration.GetSection(CfgOptions.SectionName).Get<CfgOptions>();

            _cfg.Validate();
            _cfg.Coerce();

            if (_cfg.Help)
            {
                PrintHelp();
                return;
            }

            if (_cfg.Service)
            {
                app.MapGet("/", HandleHttpRequest);

                app.Run();
            }
            else
            {
                DoConsole();
            }
        }

        static void PrintHelp()
        {
            using (var streamReader = new StreamReader("readme.txt"))
            {
                var str = streamReader.ReadToEnd();

                Console.WriteLine(str);
            }
        }

        static void DoConsole()
        {
            if(_cfg.Parse)
            {
                DoParse();
            }

            if (_cfg.Print)
            {
                var logLines = DoQuery();

                foreach(var logLine in logLines)
                {
                    var h1 = _cfg.ByDate ? $"{logLine.Header.DateTime}" : $"{logLine.Header.IPAddress}";
                    var h2 = _cfg.ByDate ? $"{logLine.Header.IPAddress}" : $"{logLine.Header.DateTime}";

                    Console.WriteLine($"==> {h1} ==> {h2}");
                    Console.WriteLine($"  **> {logLine.Body}");
                }

                Console.WriteLine("\nPress ANY key to exit.");
                Console.ReadKey();
            }
        }

        static void DoParse()
        {
            var logFileIsEmpty = true;

            var minDateTime = DateTime.MaxValue;
            var maxDateTime = DateTime.MinValue;

            using (var streamReader = new StreamReader(_cfg.LogFileName))
            {
                var line = streamReader.ReadLine();
                while (line != null)
                {
                    logFileIsEmpty = false;

                    var logLine = ParseLogLine(line);

                    if (logLine.Header.DateTime < minDateTime)
                    {
                        minDateTime = logLine.Header.DateTime;
                    }

                    if (logLine.Header.DateTime > maxDateTime)
                    {
                        maxDateTime = logLine.Header.DateTime;
                    }

                    line = streamReader.ReadLine();
                }
            }

            if (logFileIsEmpty) return;

            var connStr = _cfg.PostgresDb.GetConnStr();

            using (var dbConn = new NpgsqlConnection(connStr))
            using (var streamReader = new StreamReader(_cfg.LogFileName))
            {
                dbConn.Open();

                var minDateTimeStr = DateTimeToSqlStr(minDateTime);
                var maxDateTimeStr = DateTimeToSqlStr(maxDateTime);

                var sql = $"DELETE FROM logLine WHERE timestamp BETWEEN {minDateTimeStr} AND {maxDateTimeStr}";

                using (var cmd = new NpgsqlCommand(sql, dbConn))
                {
                    cmd.ExecuteNonQuery();
                }

                var line = streamReader.ReadLine();
                while (line != null)
                {
                    var logLine = ParseLogLine(line);

                    var ipAddress = logLine.Header.IPAddress;
                    var timestamp = DateTimeToSqlStr(logLine.Header.DateTime);
                    var body = logLine.Body;

                    var bodyQuote = "body47502DB6186A474FB751DF4ADF3A4F05";

                    sql = $"INSERT INTO logLine (ipaddress, timestamp, body) VALUES ('{ipAddress}', {timestamp}, ${bodyQuote}${body}${bodyQuote}$)";

                    using (var cmd = new NpgsqlCommand(sql, dbConn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    line = streamReader.ReadLine();
                }
            }
        }

        static string DateTimeToSqlStr(DateTime dt)
        {
            return $"'{dt.Year}-{dt.Month}-{dt.Day} {dt.Hour}:{dt.Minute}:{dt.Second}'";
        }

        static LogLine[] DoQuery()
        {
            var connStr = _cfg.PostgresDb.GetConnStr();

            using (var dbConn = new NpgsqlConnection(connStr))
            {
                dbConn.Open();

                var selectPart = "SELECT ipaddress, to_char(timestamp, 'YYYY-MM-DD HH24:MI:SS') as timestamp, body FROM logline";

                var wherePart = "WHERE 1=1";

                if (_cfg.GetFilterIp() != null)
                {
                    wherePart += $" AND ipaddress = '{_cfg.GetFilterIp()}'";
                }

                if (_cfg.GetFilterDate() != null)
                {
                    var filterDateStr = DateTimeToSqlStr(_cfg.GetFilterDate().Value);

                    wherePart += $" AND timestamp::date = {filterDateStr}::date";
                }

                if (_cfg.GetFilterDate1() != null)
                {
                    var filterDate1Str = DateTimeToSqlStr(_cfg.GetFilterDate1().Value);
                    var filterDate2Str = DateTimeToSqlStr(_cfg.GetFilterDate2().Value);

                    wherePart += $" AND timestamp::date BETWEEN {filterDate1Str}::date and {filterDate2Str}::date";
                }

                var orderPart = "ORDER BY " + (_cfg.ByDate ? "timestamp, ipaddress" : "ipaddress, timestamp");

                var sql = $"{selectPart} {wherePart} {orderPart}";

                var logLines = new List<LogLine>();

                using (var cmd = new NpgsqlCommand(sql, dbConn))
                {
                    var rdr = cmd.ExecuteReader();

                    while (rdr.Read())
                    {
                        var logLineHeader = new LogLineHeader();
                        
                        logLineHeader.IPAddress = IPAddress.Parse($"{rdr["ipaddress"]}").ToString();

                        var flags = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

                        var timestampStr = $"{rdr["timestamp"]}";

                        logLineHeader.DateTime = DateTime.ParseExact(timestampStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, flags);

                        var logLine = new LogLine();

                        logLine.Header = logLineHeader;
                        logLine.Body = $"{rdr["body"]}";

                        logLines.Add(logLine);
                    }
                }

                return logLines.ToArray();
            }
        }

        /// <summary>
        /// Парсит access строку лога Apache в экземпляр класса LogLineHeader.
        /// </summary>
        /// <param name="logLine">Строка access лога Apache.</param>
        /// <returns>Экземпляр класса LogLineHeader.</returns>
        static LogLine ParseLogLine(string logLineStr)
        {
            var pattern = _cfg.LogLine.Pattern;
            
            var ipAddressReplacement = _cfg.LogLine.Replacement.IpAdress;
            var dateTimeReplacement = _cfg.LogLine.Replacement.DateTime;
            var bodyReplacement = _cfg.LogLine.Replacement.Body;

            var regex = new Regex(pattern);

            var match = regex.Match(logLineStr);

            var ipAddressStr = match.Result(ipAddressReplacement);
            var dateTimeStr = match.Result(dateTimeReplacement);
            var bodyStr = match.Result(bodyReplacement);

            var ipAddress = IPAddress.Parse(ipAddressStr);
            var dateTime = ParseDateTime(dateTimeStr);

            var logLineHeader = new LogLineHeader();

            logLineHeader.IPAddress = ipAddress.ToString();
            logLineHeader.DateTime = dateTime;

            var logLine = new LogLine();

            logLine.Header = logLineHeader;
            logLine.Body = bodyStr;

            return logLine;
        }

        /// <summary>
        /// Парсит строку в структуру данных DateTime.
        /// </summary>
        /// <param name="str">Содержащая дату и время строка.</param>
        /// <returns>Структура данных DateTime.</returns>
        static DateTime ParseDateTime(string str)
        {
            var dateTimeReplacement = _cfg.LogLine.Replacement.DateTimeReplacement;

            var pattern = dateTimeReplacement.Pattern;
            var replacement = dateTimeReplacement.Replacement;
            var format = dateTimeReplacement.ParseFormat;

            Regex regex = new Regex(pattern);
            Match match = regex.Match(str);
            str = match.Result(replacement);

            DateTime dateTime = DateTime.ParseExact(str, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

            return dateTime;
        }

        static HttpResult HandleHttpRequest(
            [FromQuery(Name = "ByDate")] bool? byDate,
            [FromQuery(Name = "FilterIp")] string filterIp,
            [FromQuery(Name = "FilterDate")] string filterDate,
            [FromQuery(Name = "FilterDate1")] string filterDate1,
            [FromQuery(Name = "FilterDate2")] string filterDate2)
        {
            var httpResult = new HttpResult();

            try
            {
                var cfg = new CfgOptions();

                if (byDate != null)
                {
                    cfg.ByDate = byDate.Value;
                }

                if (filterIp != null)
                {
                    cfg.FilterIp = filterIp;
                }

                if (filterDate != null)
                {
                    cfg.FilterDate = filterDate;
                }

                if (filterDate1 != null)
                {
                    cfg.FilterDate1 = filterDate1;
                }

                if (filterDate2 != null)
                {
                    cfg.FilterDate2 = filterDate2;
                }

                cfg.Validate();
                cfg.Coerce();

                _cfg.ByDate = cfg.ByDate;

                _cfg.FilterIp = null;
                _cfg.FilterDate = null;
                _cfg.FilterDate1 = null;
                _cfg.FilterDate2 = null;

                if (cfg.GetFilterIp() != null)
                {
                    _cfg.FilterIp = cfg.GetFilterIp().ToString();
                }

                if (cfg.GetFilterDate() != null)
                {
                    _cfg.FilterDate = cfg.GetFilterDate().Value.ToString("dd.MM.yyyy");
                }

                if (cfg.GetFilterDate1() != null)
                {
                    _cfg.FilterDate1 = cfg.GetFilterDate1().Value.ToString("dd.MM.yyyy");
                }

                if (cfg.GetFilterDate2() != null)
                {
                    _cfg.FilterDate2 = cfg.GetFilterDate2().Value.ToString("dd.MM.yyyy");
                }

                httpResult.LogLines = DoQuery();
            }
            catch(Exception ex)
            {
                httpResult.Error = ex.Message;
            }

            return httpResult;
        }
    }
}
