using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whispbot.Databases;
using YellowMacaroni.Discord.Cache;
using YellowMacaroni.Discord.Core;
using YellowMacaroni.Discord.Extentions;

namespace Whispbot
{
    public static partial class Procedures
    {
        public async static Task PostClockin(long guildId, long moderatorId, ShiftType type, Shift shift)
        {
            Guild? thisGuild = await DiscordCache.Guilds.Get(guildId.ToString());
            if (thisGuild is null) return;

            Member? moderator = await thisGuild.members.Get(moderatorId.ToString());

            if (type.role_id is not null && moderator is not null)
            {
                if (!(moderator.roles ?? []).Contains($"{type.role_id}"))
                {
                    Task _ = moderator.AddRole(type.role_id?.ToString() ?? "", $"Clocked in to shift type '{type.name}'.");
                }
            }

            GuildConfig? config = await WhispCache.GuildConfig.Get(guildId.ToString());
            if (config is null) return;

            string? logChannelId = (type.log_channel_id ?? config.default_shift_log_channel)?.ToString();
            if (logChannelId is null) return;

            Channel? logChannel = await DiscordCache.Channels.Get(logChannelId);
            if (logChannel is null) return;

            Task __ = logChannel.Send(new MessageBuilder()
            {
                embeds = [
                    new EmbedBuilder()
                    {
                        author = new EmbedAuthor()
                        {
                            name = $"@{moderator?.user?.username ?? "err"} ({moderatorId})",
                            icon_url = moderator?.avatar_url ?? moderator?.user?.avatar_url
                        },
                        title = "{string.title.clockin}",
                        description = $"<@{moderatorId}> {{string.content.clockin}} '{type.name}'.",
                        color = (int)(new Color(0, 150, 0)),
                        footer = new EmbedFooter() { text = $"ID: {shift.id}" }
                    }
                ]
            });
        }

        public static (Shift?, string?) Clockin(long guildId, long moderatorId, ShiftType type)
        {
            Shift? thisShift = null;
            try
            {
                thisShift = Postgres.SelectFirst<Shift>(
                    @"INSERT INTO shifts (guild_id, moderator_id, type) VALUES (@1, @2, @3) RETURNING *;",
                    [guildId, moderatorId, type.id]
                );
            }
            catch (Exception ex)
            {
                if (ex.Data["SqlState"]?.ToString() == "23505")
                {
                    return (null, "{string.errors.clockin.already}");
                }
            }

            if (thisShift is null)
            {
                return (null, "{string.errors.clockin.dbfailed}");
            }

            Task.Run(async () => await PostClockin(guildId, moderatorId, type, thisShift));

            return (thisShift, null);
        }
    }

    public class Shift
    {
        public long id = 0;
        public long guild_id = 0;
        public long moderator_id = 0;
        public long type = 0;
        public DateTimeOffset start_time = DateTimeOffset.UtcNow;
        public DateTimeOffset? end_time = null;
    }
}
