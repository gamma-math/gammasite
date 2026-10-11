using System;
using System.IO;

namespace GamMaSite.Configuration
{
    internal static class LocalEnvironment
    {
        public static void Load()
        {
            var path = FindEnvironmentFile();
            if (path == null)
            {
                return;
            }

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                {
                    line = line[7..].TrimStart();
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();
                if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value[1..^1];
                }

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            LoadMySqlConnectionString();
        }

        private static void LoadMySqlConnectionString()
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")))
            {
                return;
            }

            var host = Environment.GetEnvironmentVariable("MYSQL_HOST");
            var port = Environment.GetEnvironmentVariable("MYSQL_PORT");
            var database = Environment.GetEnvironmentVariable("MYSQL_DATABASE");
            var username = Environment.GetEnvironmentVariable("MYSQL_USER");
            var password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(username) || password == null)
            {
                return;
            }

            var connectionString = $"Server={host};Port={port ?? "3306"};Database={database};User={username};Password={password};";
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
        }

        private static string FindEnvironmentFile()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (var level = 0; level < 6 && directory != null; level++, directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, ".env.local");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
