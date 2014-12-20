using System;
using System.IO;

namespace WarlightIRC
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                WarlightBot rt = new WarlightBot("irc.tweakers.net", 6667);
                rt.SetNick("WarlightBot");
                rt.JoinChannel("#brechthou");
                rt.Connect();
            }
            catch (Exception ioe)
            {
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\errors", String.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTime.Now) + ioe.Message + "\r\n" + ioe.InnerException.Message + "\r\n");
            }
        }
    }
}
