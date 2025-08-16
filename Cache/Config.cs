using Npgsql;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whispbot.Databases;
using YellowMacaroni.Discord.Cache;
using YellowMacaroni.Discord.Extentions;

namespace Whispbot
{
    public partial class WhispCache
    {
        public static readonly Collection<GuildConfig> GuildConfig = new(async (key, args) =>
        {
            GuildConfig? existingRecord = Postgres.SelectFirst<GuildConfig>(
              @"SELECT * FROM guild_config WHERE id = @1;",
              [long.Parse(key)]
            );

            if (existingRecord is not null) return existingRecord;

            return Postgres.SelectFirst<GuildConfig>(
                @"INSERT INTO guild_config (id, name) VALUES (@1, @2) RETURNING *;",
                [long.Parse(key), DiscordCache.Guilds.Get(key).WaitFor()?.name]
            );
        });

        public static readonly Collection<List<ShiftType>> ShiftTypes = new(async (key, args) =>
        {
            return Postgres.Select<ShiftType>(
                @"SELECT * FROM shift_types WHERE guild_id = @1;",
                [long.Parse(key)]
            );
        });
    }

    public class GuildConfig
    {
        public long id = 0;
        public string? name;
        public string? icon_url;
        public BotVersion version = BotVersion.Production;
        public long enabled_modules = 0;

        public long? default_shift_log_channel = null;
    }

    public enum BotVersion
    {
        Production = 0,
        Beta = 1,
        Alpha = 2
    }

    public class ShiftType
    {
        public long id = 0;
        public long guild_id = 0;
        public string name = "New Shift Type";
        public bool is_default = false;
        public DateTimeOffset created_at = DateTimeOffset.UtcNow;
        public DateTimeOffset updated_at = DateTimeOffset.UtcNow;
        public bool is_deleted = false;
        public List<string> triggers = [];
        public long? role_id = null;
        public long? log_channel_id = null;
    }
}
