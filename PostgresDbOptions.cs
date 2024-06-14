namespace Praktika
{
    public class PostgresDbSqlOptions
    {
        public string Host { get; set; } = "localhost";
        public ushort Port { get; set; } = 5432;
        public string Database { get; set; } = "Praktika";
        public string User { get; set; } = "postgres";
        public string Password { get; set; }

        public string GetConnStr()
        {
            return $"Host={Host}; Port={Port}; Database={Database}; User ID={User}; Password={Password}";
        }
    }
}
