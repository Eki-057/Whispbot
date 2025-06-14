using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whispbot.Databases
{
    public static class SentryConnection
    {
        public static void Init()
        {
            string? sentry_dsn = Environment.GetEnvironmentVariable("SENTRY_DSN");

            if (sentry_dsn is null)
            {
                Log.Error("Could not connect to sentry, no SENTRY_DSN environment variable.");
                return;
            }

            try
            {
                SentrySdk.Init(options =>
                {
                    options.Dsn = sentry_dsn;

                    options.Debug = false;

                    options.AutoSessionTracking = true;
                });
                Log.Information("Initialized sentry");
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to initialize sentry, not fatal");
                Log.Warning(ex.Message);
            }
        }
    }
}
