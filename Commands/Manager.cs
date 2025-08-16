using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Whispbot.Commands.General;
using Whispbot.Commands.Shifts;
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

            RegisterCommand(new Clockin());
            RegisterCommand(new Clockout());

            RegisterStaffCommand(new Test());
            RegisterStaffCommand(new SQL());
            RegisterStaffCommand(new UpdateLanguages());

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

        private int? _maxLength = null;
        public int MaxLength
        {
            get
            {
                _maxLength ??= _commands.Max(c => c.Aliases.Max(a => a.Split(" ").Length));
                return _maxLength ?? 0;
            }
        }

        public void HandleMessage(Client client, Message message)
        {
            string prefix = Config.IsDev ? "a!" : "b!";
            string mention = $"<@{client.readyData?.user.id}>";

            string staffPrefix = Config.staffPrefix;

            if (message.content.StartsWith(mention)) prefix = mention;

            if (message.content.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
            {
                List<string> args = [.. message.content[prefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries)];
                string content = args.Join(" ");

                Command? command = null;
                for (int len = MaxLength; len > 0; len--)
                {
                    Command? activeCommand = _commands.Find(c =>
                    {
                        foreach (string alias in c.Aliases)
                        {
                            int length = alias.Split(" ").Length;
                            if (length == len && (content.StartsWith($"{alias} ", StringComparison.CurrentCultureIgnoreCase) || content.Equals(alias, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                args.RemoveRange(0, length);
                                return true;
                            }
                        }
                        return false;
                    });
                    if (activeCommand is not null)
                    {
                        command = activeCommand;
                        break;
                    }
                }

                MatchCollection matches = Regex.Matches(args.Join(" "), @"--(\w+)");
                List<string> flags = [.. matches.Select(m => m.Groups[1].Value)];
                args = [..args.Where(a => !flags.Contains(a))];

                command?.ExecuteAsync(new CommandContext(client, message, args, flags));
            }
            else if 
            (
                message.content.StartsWith(staffPrefix, StringComparison.CurrentCultureIgnoreCase)
                //                          |   Support Server  |             ->          |     Member    |               ->            |  Has Staff Role?  |
                && (DiscordCache.Guilds.Get("1096509172784300174").WaitFor()?.members.Get(message.author.id).WaitFor()?.roles?.Contains("1256333207599841435") ?? false)
            )
            {
                List<string> args = [.. message.content[staffPrefix.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries)];
                string content = args.Join(" ");

                Command? command = null;
                for (int len = MaxLength; len > 0; len--)
                {
                    Command? activeCommand = _staffCommands.Find(c =>
                    {
                        foreach (string alias in c.Aliases)
                        {
                            int length = alias.Split(" ").Length;
                            if (length == len && (content.StartsWith($"{alias} ", StringComparison.CurrentCultureIgnoreCase) || content.Equals(alias, StringComparison.CurrentCultureIgnoreCase)))
                            {
                                args.RemoveRange(0, length);
                                return true;
                            }
                        }
                        return false;
                    });
                    if (activeCommand is not null)
                    {
                        command = activeCommand;
                        break;
                    }
                }

                MatchCollection matches = Regex.Matches(args.Join(" "), @"--(\w+)");
                List<string> flags = [.. matches.Select(m => m.Groups[1].Value)];
                args = [.. args.Where(a => !flags.Contains(a))];

                command?.ExecuteAsync(new CommandContext(client, message, args, flags));
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
