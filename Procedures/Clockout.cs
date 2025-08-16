﻿using Serilog;
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
        public async static Task PostClockout(long guildId, long moderatorId, ShiftType type, Shift shift)
        {
            Guild? thisGuild = await DiscordCache.Guilds.Get(guildId.ToString());
            if (thisGuild is null) return;

            Member? moderator = await thisGuild.members.Get(moderatorId.ToString());

            if (type.role_id is not null && moderator is not null)
            {
                if ((moderator.roles ?? []).Contains($"{type.role_id}"))
                {
                    Task _ = moderator.RemoveRole(type.role_id?.ToString() ?? "", $"Clocked out of shift type '{type.name}'.");
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
                        title = "{string.title.clockout}",
                        description = $"<@{moderatorId}> {{string.content.clockout}} '{type.name}'.",
                        color = (int)(new Color(150, 0, 0)),
                        footer = new EmbedFooter() { text = $"ID: {shift.id}" }
                    }
                ]
            });
        }

        public static (Shift?, string?) Clockout(long guildId, long moderatorId, ShiftType type)
        {
            Shift? thisShift = null;
            try
            {
                thisShift = Postgres.SelectFirst<Shift>(
                    @"UPDATE shifts SET end_time = now() WHERE moderator_id = @1 AND type = @2 RETURNING *",
                    [moderatorId, type.id]
                );
            }
            catch
            {
                return (null, null);
            }

            if (thisShift is null)
            {
                return (null, "{string.errors.clockout.dbfailed}");
            }

            Task.Run(async () => await PostClockout(guildId, moderatorId, type, thisShift));

            return (thisShift, null);
        }
    }
}
