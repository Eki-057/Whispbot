using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whispbot.Commands.General;
using Whispbot.Commands.Staff;
using YellowMacaroni.Discord.Cache;
using YellowMacaroni.Discord.Core;
using YellowMacaroni.Discord.Extentions;
using YellowMacaroni.Discord.Sharding;

namespace Whispbot.Commands
{
    public class CommandManager
    {
        private readonly List<Command> _commands = [];
        private readonly List<Command> _staffCommands = [];

        public CommandManager()
        {

            #region Commands

            RegisterCommand(new Ping());
            RegisterCommand(new About());
            RegisterCommand(new Support());

            RegisterStaffCommand(new Test());

            #endregion

        }

        public void RegisterCommand(Command command)
        {
            if (_commands.Any(c => c.Name == command.Name)) return;
            _commands.Add(command);
        }

        public void RegisterStaffCommand(Command command)
        {
            if (_staffCommands.Any(c => c.Name == command.Name)) return;
            _staffCommands.Add(command);
        }

        public void HandleMessage(Client client, Message message)
        {
            string prefix = Config.IsDev ? "a!" : "b!";
            string mention = $"<@{client.readyData?.user.id}>";

            string staffPrefix = Config.IsDev ? ">>>" : ">>";

            if (message.content.StartsWith(mention)) prefix = mention;

            if (message.content.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
            {
                List<string> args = [.. message.content[prefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries)];
                string commandName = args[0].ToLowerInvariant(); args.RemoveAt(0);

                Command? command = _commands.Find(c => c.Aliases.Contains(commandName));

                command?.ExecuteAsync(new CommandContext(client, message, args));
            }
            else if 
            (
                message.content.StartsWith(staffPrefix, StringComparison.CurrentCultureIgnoreCase)
                //                          |   Support Server  |             ->          |     Member    |               ->            |  Has Staff Role?  |
                && (DiscordCache.Guilds.Get("1096509172784300174").WaitFor()?.members.Get(message.author.id).WaitFor()?.roles?.Contains("1256333207599841435") ?? false)
            )
            {
                List<string> args = [.. message.content[staffPrefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries)];
                string commandName = args[0].ToLowerInvariant(); args.RemoveAt(0);

                Command? command = _staffCommands.Find(c => c.Aliases.Contains(commandName));

                command?.ExecuteAsync(new CommandContext(client, message, args));
            }
        }

        public void Attach(Client client)
        {
            client.MessageCreate += (c, message) =>
            {
                if (c is not Client client) return;
                HandleMessage(client, message);
            };
        }

        public void Attach(ShardingManager manager)
        {
            foreach (Shard shard in manager.shards)
            {
                Attach(shard.client);
            }
        }
    }
}
