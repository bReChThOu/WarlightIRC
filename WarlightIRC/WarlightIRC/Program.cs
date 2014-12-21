using System;
using System.IO;
using WarlightIRC.Core;

namespace WarlightIRC
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var bot = new WarlightBot("irc.tweakers.net", 6667);
                bot.JoinChannel("#brechthou");
                bot.Connect();
            }
            catch (Exception ioe)
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\errors", String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + ioe.Message + "\r\n" + ioe.InnerException.Message + "\r\n");
            }
        }
    }
}
