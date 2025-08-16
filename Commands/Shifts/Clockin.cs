using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whispbot.Databases;
using YellowMacaroni.Discord.Core;
using YellowMacaroni.Discord.Extentions;

namespace Whispbot.Commands.Shifts
{
    public class Clockin : Command
    {
        public override string Name => "Clockin";
        public override string Description => "Clock in to the given shift type.";
        public override List<string> Aliases => ["shift start", "clockin"];
        public override List<string> Usage => [];
        public override async Task ExecuteAsync(CommandContext ctx)
        {
            if (ctx.user?.id is null) return;

            if (ctx.guildId is null) // Make sure ran in server
            {
                await ctx.Reply("{emoji.cross} {string.errors.general.guildonly}");
                return;
            }

            List<ShiftType>? types = await WhispCache.ShiftTypes.Get(ctx.guildId); // Fetch shift types from cache

            if (types is null)
            {
                await ctx.Reply("{emoji.cross} {string.errors.clockin.dbfailed}"); // Database failed (does not mean no shift types)
                return;
            }

            if (types.Count == 0) // Create default shift type if none exist
            {
                types = [Postgres.SelectFirst<ShiftType>(
                    @"INSERT INTO shift_types (guild_id, is_default) VALUES (@1, true) RETURNING *;",
                    [long.Parse(ctx.guildId)]
                )];
                WhispCache.ShiftTypes.Remove(ctx.guildId); // Invalidate cache for this guild

                if (types is null || types.Count == 0) // Failed to create default shift type
                {
                    await ctx.Reply("{emoji.cross} {string.errors.clockin.dbfailed}");
                    return;
                }
            }

            ShiftType? type = ctx.args.Count > 0 ? types.Find(t => t.triggers.Contains(ctx.args[0])) : types.Find(t => t.is_default); // Find type based on arg or default if no args

            if (type is null)
            {
                await ctx.Reply("{emoji.cross} {string.errors.clockin.typenotfound}");
                return;
            }

            (Shift?, string?) result = Procedures.Clockin(long.Parse(ctx.guildId), long.Parse(ctx.user.id), type);

            await ctx.Reply(
                new MessageBuilder()
                {
                    embeds = [
                        new EmbedBuilder()
                        {
                            color = result.Item1 is not null ? (int)(new Color(0, 150, 0)) : null,
                            description = $"{(result.Item1 is not null ? "{emoji.clockedin}" : "{emoji.cross}")} {result.Item2 ?? (result.Item1 is null ? "{string.errors.clockin.failed}" : "{string.success.clockin}")}."
                        }
                    ]
                }
            );
        }
    }
}
