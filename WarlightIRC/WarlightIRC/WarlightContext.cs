using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using HtmlAgilityPack;

namespace WarlightIRC
{
    public class WarlightContext
    {
        private NetworkStream netStream;
        private StreamReader input;
        private StreamWriter output;
        private String hostname = "http://theaigames.com";
        private int port = 80;
        private void SendRawLine(String line)
        {
            output.Write(line + "\r\n");
        }
        private void SendCrlf()
        {
            output.Write("\r\n");
        }

        public RankingData GetRanking(string player)
        {
            return DownloadRankingData("/competitions/warlight-ai-challenge-2/leaderboard/global/a/", player);
        }

        private RankingData DownloadRankingData(string url, string playerName)
        {
            try
            {
                var data = new RankingData();

                var client = new WebClient();
                WebRequest.DefaultWebProxy = null;
                client.Proxy = null;

                var htmlBytes = client.DownloadData(hostname + url);
                var html = System.Text.Encoding.UTF8.GetString(htmlBytes);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var leaderbord = doc.DocumentNode
                    .SelectNodes("//table")
                    .First(section => section.Attributes.Any(attribute => attribute.Value == "leaderboard-table"));

                var players = leaderbord.ChildNodes.First().ChildNodes;

                var player = players.FirstOrDefault(pl => pl.SelectSingleNode(".//div[@class='user-name']").InnerText.Trim().Equals(playerName, StringComparison.OrdinalIgnoreCase));

                if (player == null)
                {
                    return data;
                }

                data.Players = players.Count;
                data.Name = player.SelectSingleNode(".//div[@class='user-name']").InnerText.Trim();
                data.Bot = player.SelectSingleNode(".//div[@class='bot-name']").InnerText.Trim();
                data.BotRevision = player.SelectSingleNode(".//div[@class='bot-revision']").InnerText.Trim();
                data.Rank = player.SelectSingleNode(".//td[@class='cell-table cell-table-round cell-table-rank']").InnerText.Trim();
                data.Score = player.SelectSingleNode(".//td[@class='cell-table cell-table-square']").SelectSingleNode(".//em").InnerText.Trim();
                
                return data;
            }
            catch (IOException e)
            {
                return new RankingData();
            }
        }
    }
}
