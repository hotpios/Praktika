---
Windows:
  Praktika [COMMAND]...
Linux:
  ./Praktika.exe [COMMAND]...
---
  When parse command specified Praktika parses Apache access log file (access.log)
  located in the same directory as the executable and places the parsed log lines
  in database, replacing in database all existing log lines for the range of dates
  located in log lines of this log file. Range of dates is defined as the range
  between minimum and maximum date found in the access.log file.
  When any print command specified (ip or date) Praktika prints log lines from
  database. Grouping order is defined by print commands order. There is a
  possibility to filter log lines by ip-address and date.
  When no print and no parse command specified then Praktika parses file access.log
  and then prints log lines.
  When no ip or date specified then Prakitka prints log lines grouped by date,
  then by ip.
  If parse command specified together with any of print commands, and no filter
  for print commands specified, then filter defined as access.log date range.
  If parse print command specified without parse command and no ip or date filter
  specified then current date filter assumed.
---
SQL for DB creation:

  CREATE DATABASE praktika;
  
  CREATE TABLE logline(
      ipaddress inet,
      timestamp timestamp,
      body varchar
  )

--- 
BEFORE RUNNING PROGRAM 
SET ENVIRONMENT VARIABLE Cfg__PostgresDb__Password WITH DB PASSWORD

  Windows:
    set Cfg__PostgresDb__Password=<password>
  Linux:
    export Cfg__PostgresDb__Password=<password>

---     
Possible COMMAND:

  - Print this help:

      --Cfg:Help=(True|False)

  - Log file to parse path.
  
      --Cfg:LogFileName=<relative-or-absolute-access-log-file-path>

  - Parse command:

      --Cfg:Parse=(True|False)

  - Print command:

      ---Cfg:Print=(True|False)

  - Filter by specified ip-address:

      --Cfg:FilterIp=<ip-address>

  - Print from database log lines GROUPING. ByDate=True is DEFAULT.
    Set ByDate=False to group by ip:

      --Cfg:ByDate=(True|False)

  - Filter by specified date:

      --Cfg:FilterDate=<dd.mm.yyyy>

  - Filter by date range (ends inclusive):

      --Cfg:FilterDate1=<dd.mm.yyyy>
      --Cfg:FilterDate2=<dd.mm.yyyy>

  - Start Praktika in HTTP-service mode. In service mode Praktika is an HTTP-server,
    listening for HTTP-requests on TCP-port specified in configuration. Optionally
    the TCP-port can be overriden as command line argument. By default Service=False.
    When Service=False Praktika is not HTTP-service, but console executable program.

      --Cfg:Service=(True|False)

---
EXAMPLES

  Praktika Cfg:Help=True

    Get current help.

  Praktika --Cfg:Service=True

    Run Praktika as HTTP-service.

  Praktika --Cfg:Service=True --urls "http://localhost:80"

    Run Praktika as HTTP-service with specified TCP-port.

  Praktika --Cfg:Parse=True

    Parses the access.log file, places it in database and exit.

  Praktika --Cfg:Parse=True --Cfg:LogFileName=access99.log

    Parses the access99.log file, places it in database and exit.

  Praktika

    Parses the access.log file, places it in database and prints log lines by date,
    then by ip, filtering date by parsed access.log date range.

  Praktika --Cfg:FilterIp=83.220.238.242

    Prints log lines filtered by ip.

  Praktika --Cfg:ByDate=False

    Prints log lines grouped by ip, then by date.

  Praktika --Cfg:FilterDate="11.06.2024"

    Prints log lines for date of 10.06.2024.

  Praktika --Cfg:FilterDate1="10.06.2024" --Cfg:FilterDate2="11.06.2024"

    Prints log lines for range of dates 10-11.06.2024 (ends inclusive).

---
HTTP API

  [&HTTP_COMMAND]...

  Supported HTTP method is GET. The HTTP-commands are the same as CLI-commands.

---
Possible HTTP_COMMAND:

  - Log lines GROUPING. ByDate=True is DEFAULT. Set ByDate=False to group by ip:

    &ByDate=(True|False)

  - Filter by specified ip-address:

    &FilterIp=<ip-address>

  - Filter by specified date.

    &FilterDate=<dd.mm.yyyy>

  - Filter by date range (ends inclusive):

    &FilterDate1=<dd.mm.yyyy>
    &FilterDate2=<dd.mm.yyyy>

HTTP API EXAMPLES

  http://<domain>:<port>/?ByDate=False

    Returns JSON with log lines grouped by ip, then by date.

  http://<domain>:<port>/?FilterDate=10.06.2024

    Returns JSON with log lines grouped by date, then by ip, for date of 10.06.2024.

  http://<domain>:<port>/?FilterDate1=10.06.2024&FilterDate2=11.06.2024

    Returns JSON with log lines for date range of 10-11.06.2024 (ends inclusive).
