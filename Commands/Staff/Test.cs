using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whispbot.Commands.Staff
{
    public class Test: Command
    {
        public override string Name => "Test";
        public override string Description => "A test command for staff.";
        public override List<string> Aliases => ["test"];
        public override List<string> Usage => [];
        public override async Task ExecuteAsync(CommandContext ctx)
        {
            await ctx.Reply("Hello world!");
        }
    }
}
