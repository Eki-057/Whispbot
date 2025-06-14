using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Whispbot.Commands.General;
using YellowMacaroni.Discord.Core;
using YellowMacaroni.Discord.Sharding;

namespace Whispbot.Commands
{
    public class CommandManager
    {
        private List<Command> Commands = [];

        public CommandManager()
        {

            #region Commands

            RegisterCommand(new Ping());

            #endregion

        }

        public void RegisterCommand(Command command)
        {
            if (Commands.Any(c => c.Name == command.Name)) return;
            Commands.Add(command);
        }

        public void HandleMessage(Client client, Message message)
        {
            string prefix = Config.isDev ? "b;" : "b!";
            string mention = $"<@{client.readyData?.user.id}>";

            if (message.content.StartsWith(mention)) prefix = mention;

            if (!message.content.StartsWith(prefix)) return;

            List<string> args = [.. message.content[prefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries)];
            string commandName = args[0].ToLowerInvariant(); args.RemoveAt(0);

            Command? command = Commands.Find(c => c.Aliases.Contains(commandName));

            command?.ExecuteAsync(new CommandContext(client, message, args));
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
