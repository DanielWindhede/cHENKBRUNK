using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace cHENKBRUNK
{

    public class Program
    {
        public readonly EventId BotEventId = new EventId(42, "cHENKBRUNK");

        public DiscordClient Client { get; set; }

        public string Prefix = ";;";

        private Dictionary<string, Func<DiscordClient, MessageCreateEventArgs, Task>> command;


        //Func<DiscordClient, MessageCreateEventArgs, Task> helpCommand = delegate (DiscordClient sender, MessageCreateEventArgs e)
        //{
        //    return Task.CompletedTask;
        //};

        public static void Main(string[] args)
        {
            // since we cannot make the entry method asynchronous,
            // let's pass the execution to asynchronous code
            var prog = new Program();
            prog.RunBotAsync().GetAwaiter().GetResult();
        }
        public async Task RunBotAsync()
        {
            // first, let's load our configuration file
            var json = "";
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            // next, let's load the values from that file
            // to our client's configuration
            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug
            };

            this.Prefix = configJson.CommandPrefix;
            print(configJson.CommandPrefix);

            IEnumerable<MessageCommand> exporters = typeof(MessageCommand)
                                            .Assembly.GetTypes()
                                            .Where(t => t.IsSubclassOf(typeof(MessageCommand)) && !t.IsAbstract)
                                            .Select(t => (MessageCommand)Activator.CreateInstance(t));

            command = new Dictionary<string, Func<DiscordClient, MessageCreateEventArgs, Task>>();

            foreach (var item in exporters)
            {
                MessageCommand c = (MessageCommand)item;
                command.Add(this.Prefix + c.Command, new Func<DiscordClient, MessageCreateEventArgs, Task>(c.Function));
            }


            // then we want to instantiate our client
            this.Client = new DiscordClient(config);

            // next, let's hook some events, so we know
            // what's going on
            this.Client.Ready += this.Client_Ready;
            this.Client.GuildAvailable += this.Client_GuildAvailable;
            this.Client.ClientErrored += this.Client_ClientError;
            this.Client.MessageCreated += this.Client_MessageCreated;
            // finally, let's connect and log in
            await this.Client.ConnectAsync();

            // and this is to prevent premature quitting
            await Task.Delay(-1);
        }

        private Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (!e.Message.Author.IsBot)
            {
                string content = e.Message.Content;

                int index = e.Message.Content.IndexOf(" ");
                string c = content;

                if (index >= 0)
                    c = c.Substring(index);

                if (command.ContainsKey(c))
                {
                    command[c].Invoke(sender, e);
                }
                else
                {
                    string[] links = GetLinks(e.Message.Content);
                    if (links.Length > 0)
                    {
                        string message = string.Empty;
                        for (int i = 0; i < links.Length; i++)
                        {
                            if (i == 0)
                                message += "```";
                            message += links[i] + "\n" + ((i == links.Length - 1) ? "```" : "");
                        }
                        Console.WriteLine("Added links");
                        e.Message.RespondAsync(message);
                    }
                }

                /*
                switch (e.Message.Content.StartsWith("...help")
                {
                    case "...help":
                        //sender.Guilds[0].Members[0].
                        
                        
                        if (e.Message.MentionedChannels.Count > 0)
                        {
                            if (list == null)
                                list = new List<DiscordChannel>();
                            foreach (var channel in e.Message.MentionedChannels)
                            {
                                list.Add(channel);
                            }
                        }

                        //björn 2#7296
                        //e.Message.RespondAsync("/private " + "hELLO " + e.Message.Author.Mention + "!!!!! pLEASE POST YOUR LINK FIRST TO SEE IF I WORK BEEP BOOP");
                        //await e.Message.RespondAsync("/private " + "hELLO " + e.Message.Author.Mention + "!!!!! pLEASE POST YOUR LINK FIRST TO SEE IF I WORK BEEP BOOP");
                        break;
                    case ""
                    default:
                        break;
                }
                */

                //x.Message.Content.StartsWith("b")
                /*
                string[] links = GetLinks(e.Message.Content);
                if (links.Length > 0)
                {
                    string message = string.Empty;
                    for (int i = 0; i < links.Length; i++)
                    {
                        if (i == 0)
                            message += "```";
                        message += links[i] + "\n" + ((i == links.Length - 1) ? "```" : "");
                    }
                    Console.WriteLine("Added links");
                    await e.Message.RespondAsync(message);
                }
                */
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }
        public string[] GetLinks(string str)
        {
            Regex regx = new Regex("http://|https://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);
            MatchCollection matches = regx.Matches(str);

            List<string> links = new List<string>();
            foreach (Match match in matches)
                links.Add(match.Value);

            return links.ToArray();
        }

        private Task Client_Ready(DiscordClient sender, ReadyEventArgs e)
        {
            // let's log the fact that this event occured
            sender.Logger.LogInformation(BotEventId, "Client is ready to process events.");
            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
        {
            // let's log the name of the guild that was just
            // sent to our client
            sender.Logger.LogInformation(BotEventId, $"Guild available: {e.Guild.Name}");

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }

        private Task Client_ClientError(DiscordClient sender, ClientErrorEventArgs e)
        {
            // let's log the details of the error that just 
            // occured in our client
            sender.Logger.LogError(BotEventId, e.Exception, "Exception occured");

            // since this method is not async, let's return
            // a completed task, so that no additional work
            // is done
            return Task.CompletedTask;
        }
        public void print(object o)
        {
            Console.WriteLine(o);
        }
    }

    // this structure will hold data from config.json
    public struct ConfigJson
    {
        [JsonProperty("token")]
        public string Token { get; private set; }

        [JsonProperty("prefix")]
        public string CommandPrefix { get; private set; }
    }
}
