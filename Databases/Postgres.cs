using Npgsql;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whispbot.Extensions;

namespace Whispbot.Databases
{
    public static class Postgres
    {
        private static NpgsqlConnection? _connection = null;
        private static bool _connected = false;
        private static DateTime _lastConnectionAttempt = DateTime.MinValue;
        private static readonly TimeSpan _reconnectInterval = TimeSpan.FromMinutes(2); // Retry after 2 minutes

        /// <summary>
        /// Gets the database connection. If the connection is not established or has been closed,
        /// it will attempt to reconnect if the last failed attempt was more than the reconnect interval ago.
        /// </summary>
        /// <returns>The active database connection, or null if connection is not available</returns>
        public static NpgsqlConnection? GetConnection()
        {
            // If we're connected and the connection is open, return it
            if (_connected && _connection?.State == System.Data.ConnectionState.Open)
            {
                return _connection;
            }

            // If we're not connected and the last attempt was recent, return null
            if (!_connected && DateTime.UtcNow - _lastConnectionAttempt < _reconnectInterval)
            {
                return null;
            }

            // Try to reconnect
            if (Init())
            {
                return _connection;
            }

            // Connection failed
            return null;
        }

        public static bool Init()
        {
            double start = DateTimeOffset.UtcNow.UtcTicks;
            Log.Information("Connecting to postgres...");
            _lastConnectionAttempt = DateTime.UtcNow;

            try
            {
                if (_connection != null)
                {
                    try
                    {
                        _connection.Close();
                        _connection.Dispose();
                    }
                    catch { }
                }

                string? host = Environment.GetEnvironmentVariable("DB_HOST");
                string? port = Environment.GetEnvironmentVariable("DB_PORT");
                string? username = Environment.GetEnvironmentVariable("DB_USERNAME");
                string? password = Environment.GetEnvironmentVariable("DB_PASSWORD");
                string? database = Environment.GetEnvironmentVariable("DB_DATABASE");

                var missingVars = new List<string>();
                if (string.IsNullOrEmpty(host)) missingVars.Add("DB_HOST");
                if (string.IsNullOrEmpty(port)) missingVars.Add("DB_PORT");
                if (string.IsNullOrEmpty(username)) missingVars.Add("DB_USERNAME");
                if (string.IsNullOrEmpty(password)) missingVars.Add("DB_PASSWORD");
                if (string.IsNullOrEmpty(database)) missingVars.Add("DB_DATABASE");

                if (Config.isDev)
                {
                    string? public_url = Environment.GetEnvironmentVariable("DB_PUBLIC_URL");
                    if (!string.IsNullOrEmpty(public_url) && public_url.Contains('@') && public_url.Contains(':'))
                    {
                        string[] parts = public_url.Split('@');
                        if (parts.Length > 1)
                        {
                            string[] hostParts = parts[1].Split(':');
                            if (hostParts.Length > 1)
                            {
                                host = hostParts[0];
                                string[] portParts = hostParts[1].Split('/');
                                if (portParts.Length > 0)
                                {
                                    port = portParts[0];
                                }
                            }
                        }
                    }
                    else if (missingVars.Contains("DB_HOST") || missingVars.Contains("DB_PORT"))
                    {
                        missingVars.Add("DB_PUBLIC_URL");
                    }
                }

                if (missingVars.Count > 0)
                {
                    Log.Fatal("ERROR: Missing required environment variables:");
                    foreach (var var in missingVars)
                    {
                        Log.Fatal($"  - {var}");
                    }
                    Log.Fatal("\nPlease set these environment variables and restart the application.");
                    Logger.Shutdown();

                    Environment.Exit(1);
                    return false;
                }

                string connectionString = $"Host={host};Port={port};Username={username};Password={password};Database={database};Timeout=15;CommandTimeout=30";

                _connection = new NpgsqlConnection(connectionString);

                _connection.OpenWithRetry(3, TimeSpan.FromSeconds(5));

                using (NpgsqlCommand command = new("SELECT 1", _connection))
                {
                    command.ExecuteNonQuery();
                }

                Log.Information($"Connected to postgres in {(DateTimeOffset.UtcNow.UtcTicks - start) / 10000}ms");
                _connected = true;
                return true;
            }
            catch (NpgsqlException ex)
            {
                Log.Error($"Database connection error: {ex.Message}");
                _connected = false;
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error during database connection: {ex.Message}");
                _connected = false;
                return false;
            }
        }

        /// <summary>
        /// Checks if the connection is currently established and open without running a query
        /// </summary>
        /// <returns></returns>
        public static bool IsConnected()
        {
            return _connected && _connection?.State == System.Data.ConnectionState.Open;
        }

        /// <summary>
        /// Checks if the connection is still valid
        /// </summary>
        public static bool IsConnectionValid()
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                return false;
            }

            try
            {
                using NpgsqlCommand command = new("SELECT 1", _connection);
                command.ExecuteScalar();
                return true;
            }
            catch
            {
                _connected = false;
                return false;
            }
        }

        public static List<T>? Select<T>(string sql) where T : new()
        {
            var connection = GetConnection();
            if (connection is null) return null;

            using var command = new NpgsqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            return reader.ToList<T>();
        }

        public static T? SelectFirst<T>(string sql) where T : new()
        {
            var connection = GetConnection();
            if (connection is null) return default;

            using var command = new NpgsqlCommand(sql, connection);
            using var reader = command.ExecuteReader();
            return reader.FirstOrDefault<T>();
        }

        public static int Execute(string sql)
        {
            var connection = GetConnection();
            if (connection is null) return -1;

            using var command = new NpgsqlCommand(sql, connection);
            return command.ExecuteNonQuery();
        }
    }
}