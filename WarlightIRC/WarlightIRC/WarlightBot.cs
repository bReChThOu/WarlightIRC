using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarlightIRC
{
    public class WarlightBot : Bot
    {
        private String version = "1.5";
	    private String date = "21/02/2013";

        public WarlightBot(String server, int port) : base(server, port)
        {
	    }

        public new void Connect()
        {
            base.Connect();
        }

        public new void SetNick(String nick)
        {
            base.SetNick(nick);
        }

        public new void JoinChannel(String channel)
        {
            base.JoinChannel(channel);
        }

	    protected override void OnChannelMessage(String channel, String nick, String[] args) 
        {
            String command = args[0].ToLowerInvariant().Substring(1);
            int nbrOfArgs = args.Length;
            String argumentsString = string.Empty;
            for (int i = 1; i < nbrOfArgs; i++) {
				argumentsString += args[i] + "+";
			}

            switch (command)
            {
                case "rank":
                    if (nbrOfArgs < 2) { SendMsg(channel, "8,1Warlight 4» 7Usage: 08!rank <player>"); }
                    else 
                    {
                        var ctx = new WarlightContext();
                        var data = ctx.GetRanking(args[1]);

                        if (data.Name != null)
                        {
                            SendMsg(channel, string.Format("8,1Warlight 4» 7{0}: 08{1} {2} 7is ranked 08{3}7/08{4} 7with score 08{5}7.", data.Name, data.Bot, data.BotRevision, data.Rank, data.Players, data.Score));
                        }
                        else
                        {
                            SendMsg(channel, "8,1Warlight 4» 7Player not found.");
                        }
                    }
                    
                    break;
                
            }

	    }
    }
}
