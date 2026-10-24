using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using Serilog;

namespace Whispbot.Tools.Bot;

internal static class GatewayWatchdog
{
    public static void Start(DiscordShardedClient client, int connectionTimeoutMs)
    {
        var recoveryTimeout = TimeSpan.FromMinutes(2);
        var startupTimeout = TimeSpan.FromMilliseconds(Math.Max(
            recoveryTimeout.TotalMilliseconds,
            connectionTimeoutMs + (Math.Max(0, client.Shards.Count - 1) * 5_000L) + 60_000L));
        var policy = new GatewayRecoveryPolicy(recoveryTimeout, startupTimeout);

        new Thread(() =>
        {
            var clock = Stopwatch.StartNew();
            while (!Shutdown.IsShuttingDown)
            {
                foreach (var shard in client.Shards)
                {
                    var state = shard.ConnectionState;
                    if (policy.NeedsRestart(shard.ShardId, state == ConnectionState.Connected,
                        clock.Elapsed, Shutdown.IsShuttingDown))
                    {
                        Shutdown.RestartForGatewayFailure(shard.ShardId, state);
                        return;
                    }
                }

                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }) { IsBackground = true, Name = "Gateway Watchdog" }.Start();

        Log.Information("Gateway watchdog started: recovery timeout {RecoverySeconds}s, startup timeout {StartupSeconds}s",
            recoveryTimeout.TotalSeconds, startupTimeout.TotalSeconds);
    }
}
