using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace cHENKBRUNK
{
    public abstract class MessageCommand
    {
        public abstract string Command { get; }
        public virtual Task Function(DiscordClient sender, MessageCreateEventArgs e)
        {
            return Task.CompletedTask;
        }
    }
    public class HelpMessageCommand : MessageCommand
    {
        public override string Command { get { return "help"; } }
        //public override string Command => "help";

        public override Task Function(DiscordClient sender, MessageCreateEventArgs e)
        {
            string message = "hELLO " + e.Message.Author.Mention + "!!!!! pLEASE POST YOUR LINK FIRST TO SEE IF I WORK BEEP BOOP";

            e.Guild.GetMemberAsync(e.Message.Author.Id).Result.SendMessageAsync(message);

            return Task.CompletedTask;
        }
    }
    
    public class TestMessageCommand : MessageCommand
    {
        public override string Command { get { return "test"; } }
        //public override string Command => "help";

        public override Task Function(DiscordClient sender, MessageCreateEventArgs e)
        {
            string message = "hELLO " + e.Message.Author.Mention + "!!!!! tHIS IS A RESPONSE";

            e.Message.RespondAsync(message);

            return Task.CompletedTask;
        }
    }
    
}
