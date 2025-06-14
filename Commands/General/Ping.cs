using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whispbot.Commands.General
{
    public class Ping: Command
    {
        public override string Name => "Ping";
        public override string Description => "Check the status of the bot.";
        public override List<string> Aliases => ["ping"];
        public override async Task ExecuteAsync(CommandContext ctx)
        {
            await ctx.Reply($"Pong! {ctx.client.ping}ms");
        }
    }
}
