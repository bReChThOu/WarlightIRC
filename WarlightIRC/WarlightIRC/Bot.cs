using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace WarlightIRC
{
    public abstract class Bot
    {
        private string Nick
        {
            get
            {
                if (nick == null)
                {
                    nick = ConfigurationManager.AppSettings["Nick"];
                }

                return nick;
            }
            set
            {
                nick = value;
            }
        }

        private string nick = null;

        private TcpClient clientSocket = null;
        private bool quit = false;

        private long startDate = DateTime.Now.Ticks / 10000;
        private String currentInstance = "0";

        private NetworkStream netStream;
        private StreamReader input;
        private StreamWriter output;

        private String hostname;
        private int port;

        private List<String> channels = new List<String>();

        public Bot(String hostname, int port)
        {
            this.hostname = hostname;
            this.port = port;
        }
        public void Connect()
        {
            clientSocket = new TcpClient(hostname, port);
            netStream = clientSocket.GetStream();
            output = new StreamWriter(netStream);
            output.AutoFlush = true;
            input = new StreamReader(netStream);
            SendRawLine("NICK " + Nick);
            SendRawLine("USER " + Nick + " 8 * :" + Nick);
            String inputLine;
            while ((inputLine = input.ReadLine().Trim()) != null && !quit)
            {
                String pat = "\\s";
                String[] line = Regex.Split(inputLine, pat);
                String code = line[1];
                if (code.Equals("JOIN"))
                {
                    String channel = line[2].Substring(1);
                    ParseJoin(line[0], channel);
                }
                else if (code.Equals("PART"))
                {
                    String channel = line[2];
                    ParsePart(line[0], channel);
                }
                else if (code.Equals("QUIT"))
                {
                    String quitMessage = line[2].Substring(1);
                    ParseQuit(line[0], quitMessage);
                }
                else if (code.Equals("NICK"))
                {
                    String newNick = line[2].Substring(1);
                    ParseNickChange(line[0], newNick);
                }
                else if (code.Equals("001"))
                { //Connected to the irc server.
                    Console.WriteLine("[" + currentInstance + "] Connected.");
                    Nick = line[2];
                    JoinChannels();
                }
                else if (code.Equals("433"))
                { //Nickname is already in use.
                    Nick += "a";
                    SendRawLine("NICK " + Nick);
                }
                else if (inputLine.Substring(0, 6).Equals("PING :"))
                {
                    SendRawLine("PONG " + inputLine.Substring(5));
                }
                else if (code.Equals("PRIVMSG"))
                {
                    String[] msg = new String[line.Length - 3];
                    msg[0] = line[3].Substring(1);
                    for (int i = 1; i < msg.Length; i++)
                    {
                        msg[i] = line[i + 3];
                    }
                    String _nick = Regex.Split(line[0], "!")[0];
                    _nick = _nick.Replace(":", "");
                    Privmsg(_nick, line[2], msg);
                }
            }

            output.Close();
            input.Close();
            clientSocket.Close();
        }


        private void SendRawLine(String inputLine)
        {
            try
            {
                output.Write(inputLine + "\r\n");
            }
            catch (IOException ioe)
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + "/errors", String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + ioe.Message + "\r\n" + ioe.InnerException.Message + "\r\n");
            }
        }


        protected void JoinChannel(String channel)
        {
            if (!channels.Contains(channel))
            {
                channels.Add(channel);
            }
        }
        private void JoinChannels()
        {
            for (int i = 0; i < channels.Count; i++)
            {
                Join(channels[i]);
            }
        }
        private void Join(String chan)
        {
            SendRawLine("JOIN " + chan);
            File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + chan, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " *" + nick + "  joined " + chan + "\r\n");

        }
        private void Part(String chan)
        {
            SendRawLine("PART " + chan);
            File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + chan, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " *" + nick + "  parted " + chan + "\r\n");
        }


        private void Privmsg(String nick, String source, String[] args)
        {
            //Check if it's a channel message or a private message
            if (source.Substring(0, 1).Equals("#"))
            {
                try
                {
                    ParseChannelMessage(nick, source, args);
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                ParsePrivateMessage(nick, args);
            }
        }
        private void ParsePrivateMessage(String nick, String[] args)
        {
            //Build message
            String msg = "";
            for (int i = 0; i < args.Length; i++)
            {
                msg += args[i] + " ";
            }
            if (Regex.IsMatch(msg, "(.*)"))
            { //CTCP msg
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + nick, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " <" + nick + "> " + Regex.Match(msg, "(.*)") + "\r\n");
            }
            else
            { //normal msg
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + nick, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " <" + nick + "> " + msg + "\r\n");
                //onPrivateMessage(nick, msg);
            }
        }

        private void ParseChannelMessage(String nick, String channel, String[] args)
        {
            //Build message
            String msg = "";
            for (int i = 0; i < args.Length; i++)
            {
                msg += args[i] + " ";
            }
            if (Regex.IsMatch(msg, "ACTION(.*)"))
            { //action msg
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + channel, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " <" + nick + "> " + Regex.Match(msg, "ACTION(.*)") + "\r\n");
            }
            else
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + channel, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " <" + nick + "> " + msg + "\r\n");
                //Basic commands
                 if (args[0].Equals("!quit", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Allowed(nick, "join"))
                    {
                        SendMsg(channel, "Exiting...goodbye!");
                        QuitInstance();
                    }
                }
                else if (args[0].Equals("!join", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Allowed(nick, "join"))
                    {
                        if (args.Length < 2)
                        {
                            SendMsg(channel, "8,1JOiN 05-> 07!join <channel>");
                        }
                        else
                        {
                            Join(args[1]);
                        }
                    }
                }
                else if (args[0].Equals("!part", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Allowed(nick, "part"))
                    {
                        if (args.Length < 2)
                        {
                            SendMsg(channel, "8,1PART 05-> 07!part <channel>");
                        }
                        else
                        {
                            Part(args[1]);
                        }
                    }
                }
                else if (args[0].Equals("!nick", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Allowed(nick, "nick"))
                    {
                        if (args.Length < 2)
                        {
                            SendMsg(channel, "8,1NiCK 05-> 07!nick <nick>");
                        }
                        else
                        {
                            this.nick = args[1];
                            SendRawLine("NICK " + nick);
                        }
                    }
                }
                else if (args[0].Equals("!uptime", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Allowed(nick, "uptime"))
                    {
                        String output = "08,01 UPTiME 05-> 07" + GetUptime();
                        SendMsg(channel, output);
                    }
                }
            }
            OnChannelMessage(channel, nick, args);
        }

        public void SendMsg(String chan, String msg)
        {
            SendRawLine("PRIVMSG " + chan + " :" + msg);
        }
        public void SendNotice(String user, String msg)
        {
            SendRawLine("NOTICE " + user + " :" + msg);
        }

        private void QuitInstance()
        {
            SendRawLine("QUIT :");
            quit = true;
        }

        public bool Allowed(String nick, String command)
        {
            bool allowed = false;
            if (command.Equals("warlight"))
            {
                allowed = true;
            }
            else if (command.Equals("nick") || command.Equals("senddebug") || command.Equals("quit") || command.Equals("part") || command.Equals("join") || command.Equals("clone") || command.Equals("killinstance") || command.Equals("listinstances"))
            {
                if (nick.Equals("bReChThOu"))
                {
                    allowed = true;
                }
            }
            if (!allowed)
            {
                Console.WriteLine("[" + currentInstance + "] Commmand " + command + " not allowed by " + nick);
            }
            return allowed;
        }

        public String GetUptime()
        {
            long currentDate = DateTime.Now.Ticks / 10000;
            String timestamp = "";
            long diff = (currentDate - startDate) / 1000;
            long d = diff / 86400;
            if (d != 0) { timestamp += "07" + d + "08d "; }
            diff -= d * 86400;
            long h = diff / 3600;
            if (h != 0) { timestamp += "07" + h + "08u "; }
            diff -= h * 3600;
            long m = diff / 60;
            if (m != 0) { timestamp += "07" + m + "08m "; }
            diff -= m * 60;
            long s = diff;
            if (s != 0) { timestamp += "07" + s + "08s"; }
            return timestamp;
        }



        private void ParseJoin(String userhost, String channel)
        {
            String _nick = Regex.Split(userhost, "!")[0]; //Parse nick
            _nick = _nick.Replace(":", "");
            String _ident = Regex.Split(userhost, "!")[1];
            String _host = Regex.Split(_ident, "@")[1]; //Parse userhost
            _ident = Regex.Split(_ident, "@")[0]; //Parse ident
            if (_nick.Equals(nick, StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("[" + currentInstance + "] Joined " + channel);
            }
            else
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + channel, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " *" + _nick + "  joined " + channel + "\r\n");
                Console.WriteLine("[" + currentInstance + "] " + _nick + " ! " + _ident + " @ " + _host + " joined " + channel);
            }
            //onJoinChannel(_nick, channel);
        }
        private void ParsePart(String userhost, String channel)
        {
            String _nick = Regex.Split(userhost, "!")[0]; //Parse nick
            _nick = _nick.Replace(":", "");
            String _ident = Regex.Split(userhost, "!")[1];
            String _host = Regex.Split(_ident, "@")[1]; //Parse userhost
            _ident = Regex.Split(_ident, "@")[0]; //Parse ident
            File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + channel, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " *" + _nick + "  joined " + channel + "\r\n");
            //onPart(userhost, channel);
        }
        private void ParseNickChange(String userhost, String newNick)
        {
            String _nick = Regex.Split(userhost, "!")[0]; //Parse nick
            _nick = _nick.Replace(":", "");
            String _ident = Regex.Split(userhost, "!")[1];
            String _host = Regex.Split(_ident, "@")[1]; //Parse userhost
            _ident = Regex.Split(_ident, "@")[0]; //Parse ident
            foreach (String channel in channels)
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + channel, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " *" + _nick + "  changed nick to " + newNick + "\r\n");
            }
            //onNickChanged(userhost, newNick);
        }

        private void ParseQuit(String userhost, String quitMessage)
        {
            String _nick = Regex.Split(userhost, "!")[0]; //Parse nick
            _nick = _nick.Replace(":", "");
            String _ident = Regex.Split(userhost, "!")[1];
            String _host = Regex.Split(_ident, "@")[1]; //Parse userhost
            _ident = Regex.Split(_ident, "@")[0]; //Parse ident
            foreach (String channel in channels)
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\" + channel, String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + " *" + _nick + "  quit (" + quitMessage + ") \r\n");
            }
            //onQuit(userhost, quitMessage);

        }
        protected String GetNick()
        {
            return this.nick;
        }
        protected void SetNick(String nick)
        {
            this.nick = nick;
        }



        protected virtual void OnChannelMessage(String channel, String nick, String[] args) { }
        protected virtual void OnPrivateMessage(String nick, String msg) { }
        protected virtual void OnJoinChannel(String nick, String channel) { }
    }
}
