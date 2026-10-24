using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Formatting.Json;

namespace Whispbot.Tools.Logging;

public sealed class RailwayFormatter : ITextFormatter
{
    private readonly MessageTemplateTextFormatter messageFormatter = new("[Cluster {ClusterId}] {Message:lj}", null);
    private readonly MessageTemplateTextFormatter levelFormatter = new("{Level:u4}", null);
    private readonly JsonValueFormatter valueFormatter = new();

    public void Format(LogEvent logEvent, TextWriter output)
    {
        using var message = new StringWriter();
        messageFormatter.Format(logEvent, message);
        using var level = new StringWriter();
        levelFormatter.Format(logEvent, level);

        output.Write("{\"message\":");
        output.Write(JsonConvert.ToString(message.ToString()));
        output.Write(",\"level\":");
        output.Write(JsonConvert.ToString(level.ToString()));
        output.Write(",\"data\":");
        if (logEvent.Properties.TryGetValue("Data", out var data))
            valueFormatter.Format(data, output);
        else
            output.Write("null");
        output.Write(",\"error\":");
        output.Write(JsonConvert.ToString(logEvent.Exception?.ToString() ?? ""));
        output.WriteLine("}");
    }
}
