using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using YellowMacaroni.Discord.Core;
using YellowMacaroni.Discord.Sharding;
using Sentry;
using Whispbot.Commands;
using Whispbot;
using Whispbot.Databases;
using Npgsql;
using Whispbot.Extensions;
using Serilog;

Logger.Initialize();

bool dev = Config.IsDev;
if (dev) Log.Information("Running in development mode.");

string? token = dev ? Environment.GetEnvironmentVariable("DEV_TOKEN") : Environment.GetEnvironmentVariable("CLIENT_TOKEN");

if (token is null)
{
    Log.Fatal("Please set the CLIENT_TOKEN environment variable.");
    Logger.Shutdown();
    return;
}

Thread redisInitializationThread = new(new ThreadStart(() =>
{
    if (!Redis.Init()) return;
}))
{ Name = "Redis Initialization" };
redisInitializationThread.Start();

Thread postgresInitializationThread = new(new ThreadStart(() =>
{
    if (!Postgres.Init()) return;
}))
{ Name = "Postgres Initialization" };
postgresInitializationThread.Start();

Thread otherInitializationThread = new(new ThreadStart(() =>
{
    SentryConnection.Init();
}))
{ Name = "Other Initialization" };
otherInitializationThread.Start();

ShardingManager sharding = new(
    token,
    Intents.GuildMessages | Intents.MessageContent | Intents.Guilds);

CommandManager commands = new();
commands.Attach(sharding);

foreach (Shard shard in sharding.shards)
{
    shard.client.On("READY", (client, obj) =>
    {
        Log.Information("Shard {ShardId} online!", shard.id);
    });
}

Log.Information("Starting...");
sharding.Start();
sharding.Hold();
Log.Information("Stopping...");
Logger.Shutdown();