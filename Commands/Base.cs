using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YellowMacaroni.Discord.Core;

namespace Whispbot.Commands
{
    public abstract class Command
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract List<string> Aliases { get; }
        public abstract List<string> Usage { get; }
        public abstract Task ExecuteAsync(CommandContext ctx);
    }

    public class CommandContext (Client client, Message message, List<string> args)
    {
        public Client client = client;
        public Message message = message;
        public List<string> args = args;

        public Message? repliedMessage = null;

        public async Task<(Message?, DiscordError?)> Reply(MessageBuilder content)
        {
            if (message.channel is null) return (null, new(new()));

            (Message? sentMessage, DiscordError? error) = await message.channel.Send(content);

            if (sentMessage is not null) repliedMessage = sentMessage;

            return (sentMessage, error);
        }

        public async Task<(Message?, DiscordError?)> Reply(string content)
        {
            return await Reply(new MessageBuilder { content = content });
        }

        public async Task<(Message?, DiscordError?)> EditResponse(MessageBuilder content)
        {
            if (repliedMessage is not null)
            {
                return await repliedMessage.Edit(content);
            }
            else return await Reply(content);
        }

        public async Task<(Message?, DiscordError?)> EditResponse(string content)
        {
            return await EditResponse(new MessageBuilder { content = content });
        }
    }
}
