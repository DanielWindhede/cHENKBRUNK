using DSharpPlus;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace cHENKBRUNK
{
    // TODO: Look at this documentation: https://dsharpplus.github.io/articles/commands/intro.html
    public abstract class MessageCommand
    {
        //syn: channel -arg desc: Adds channel
        public abstract string Command { get; }
        public abstract string HelpArguments { get; }
        public abstract string HelpDescription { get; }
        public virtual Task Function(DiscordClient sender, MessageCreateEventArgs e)
        {
            return Task.CompletedTask;
        }
        protected string[] GetParameters(DiscordClient sender, MessageCreateEventArgs e)
        {
            string c = e.Message.Content;
            List<string> possibleArgs = new List<string>();

            Regex regx = new Regex(@"\b[\w']*\b", RegexOptions.IgnoreCase);
            MatchCollection matches = regx.Matches(c);

            foreach (var item in matches)
            {
                if (item.ToString() != "")
                    possibleArgs.Add(item.ToString());
            }
            possibleArgs.RemoveAt(0);

            foreach (var item in possibleArgs)
                Console.WriteLine(item);

            return possibleArgs.ToArray();
        }
    }
    public class HelpMessageCommand : MessageCommand
    {
        public override string Command { get { return "help"; } }
        public override string HelpArguments { get { return "-command"; } }
        public override string HelpDescription { get { return "returns a list describing syntax and purpose of given or every command"; } }

        public override Task Function(DiscordClient sender, MessageCreateEventArgs e)
        {
            string c = e.Message.Content;
            string message = "hELLO " + e.Message.Author.Mention + "!!!";

            IEnumerable<MessageCommand> exporters = typeof(MessageCommand)
                                            .Assembly.GetTypes()
                                            .Where(t => t.IsSubclassOf(typeof(MessageCommand)) && !t.IsAbstract)
                                            .Select(t => (MessageCommand)Activator.CreateInstance(t));

            var ordered = exporters.OrderBy(x => x.Command);

            bool found = false;
            string[] param = GetParameters(sender, e);
            if (param.Length > 0)
            {
                var val = ordered.Where(x => param.Contains(x.Command)).First();
                message += " iNFORMATION REGARDING COMMAND \"" + val.Command + "\":" + "\n" + GetMessage(val);
                found = true;
            }

            if (!found)
            {
                message += "hERE IS A LIST OF EVERY COMMAND\n";
                foreach (var item in ordered)
                {
                    message += GetMessage(item);
                }
            }

            e.Guild.GetMemberAsync(e.Message.Author.Id).Result.SendMessageAsync(message);
            e.Channel.DeleteMessageAsync(e.Message);
            return Task.CompletedTask;
        }

        private string GetMessage(MessageCommand command)
        {
            string message = "`" + command.Command;

            if (command.HelpArguments.Length > 0)
                message += " " + command.HelpArguments;

            message += "`";

            message += "** desc:** " + command.HelpDescription;

            message += "\n";

            return message;
        }
    }
    
    public class TestMessageCommand : MessageCommand
    {
        public override string Command { get { return "test"; } }
        public override string HelpArguments { get { return ""; } }
        public override string HelpDescription { get { return "returns a test-response"; } }
        //public override string Command => "help";

        public override Task Function(DiscordClient sender, MessageCreateEventArgs e)
        {
            string message = "hELLO " + e.Message.Author.Mention + "!!!!! tHIS IS A RESPONSE";

            e.Message.RespondAsync(message);

            return Task.CompletedTask;
        }
    }

    public class ChannelCommand : MessageCommand
    {
        private enum ParamMode
        {
            ADD,
            REMOVE
        }
        private ParamMode paramMode = ParamMode.ADD;

        public override string Command { get { return "channel"; } }
        public override string HelpArguments { get { return "-text_channel"; } }
        public override string HelpDescription { get { return "Marks given (or current if arg-less) channel as link-fetcher channel"; } }
        //public override string Command => "help";

        public override Task Function(DiscordClient sender, MessageCreateEventArgs e)
        {
            HashSet<ulong> idArray = Program.content.IdArray != null ? new HashSet<ulong>(Program.content.IdArray) : new HashSet<ulong>();

            ulong messageChannelId = 0; // it is highley unlikley a channel ID of 0 is assigned to a user-made servers text-channel
            string[] param = GetParameters(sender, e);
            if (param.Length > 0)
            {
                int index = 0;

                if (param[0] == "remove")
                {
                    index = 1;
                    paramMode = ParamMode.REMOVE;
                }

                if (param.Length - 1 >= index)
                    ulong.TryParse(GetParameters(sender, e)[index], out messageChannelId);
            }

            if (!sender.GetChannelAsync(messageChannelId).IsCompletedSuccessfully)
                messageChannelId = e.Channel.Id;

            string message = string.Empty;

            switch (paramMode)
            {
                case ParamMode.ADD:
                    if (!idArray.Contains(messageChannelId))
                    {
                        idArray.Add(messageChannelId);
                        SaveChannel(ref idArray);
                        message = sender.GetChannelAsync(messageChannelId).Result.Mention + " iS NOW MARKED AS A LINK-FETCHER CHANNEL";
                    }
                    else
                        message = sender.GetChannelAsync(messageChannelId).Result.Mention + " iS ALREADY MARKED AS A LINK-FETCHER CHANNEL";

                    break;
                case ParamMode.REMOVE:
                    if (sender.GetChannelAsync(messageChannelId).IsCompletedSuccessfully)
                    {
                        if (idArray.Contains(messageChannelId))
                        {
                            idArray.Remove(messageChannelId);

                            SaveChannel(ref idArray);

                            message = sender.GetChannelAsync(messageChannelId).Result.Mention + " iS NOW UNMARKED";
                        }
                        //else
                        //{
                        //    message = sender.GetChannelAsync(messageChannelId).Result.Mention + " iS NOT MARKED";
                        //}
                    }
                        
                    break;
                default:
                    break;
            }

            e.Message.RespondAsync(message);

            return Task.CompletedTask;
        }

        private Task SaveChannel(ref HashSet<ulong> idArray)
        {
            Program.content = new ContentJson()
            {
                //IdArray = arr.ToArray()
                IdArray = idArray
            };

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;

            StreamWriter sw = new StreamWriter(Program.GetFullContentPath);
            JsonWriter writer = new JsonTextWriter(sw);

            serializer.Serialize(writer, Program.content);

            writer.Close();
            sw.Close();

            Program.LoadContent().GetAwaiter().GetResult();

            return Task.CompletedTask;
        }
    }
}
