using Amazon;
using Amazon.S3;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Whispbot.Commands.ERLC.Commands.Debug;

namespace Whispbot.Tools.Logging
{
    public static class Logger
    {
        public static void Initialize()
        {
            var configuration = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Verbose)
                .Enrich.With(new LogEnricher());

            if (Config.isDev)
                configuration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss.fff}][{Level:u4}][Cluster {ClusterId}] {Message:lj} {Data}{NewLine}{Exception}",
                    theme: SystemConsoleTheme.Colored);
            else
                configuration.WriteTo.Console(new RailwayFormatter());

            Log.Logger = configuration.WriteTo.File(
                    "Logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 10 * 1024 * 1024, 
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 28
                )
                .CreateLogger();
        }

        public static void Shutdown()
        {
            Log.CloseAndFlush();
        }

        public static ILogger WithData(object data)
        {
            return Log.ForContext("Data", JsonConvert.SerializeObject(data));
        }
    }

    public partial class LogEnricher: ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ClusterId", Config.cluster.ToString().PadLeft(Config.replicas.Count.ToString().Length)));

            if (!logEvent.Properties.ContainsKey("Data"))
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Data", new ScalarValue(null)));
        }
    }
}
