using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using HtmlAgilityPack;
using System.Collections.Generic;

namespace WarlightIRC
{
    public class WarlightContext
    {
        private WebClient webclient;

        private String Hostname
        {
            get
            {
                return "http://theaigames.com";
            }
        }

        private WebClient WebClient
        {
            get
            {
                if (webclient == null)
                {
                    webclient = new WebClient();
                    WebRequest.DefaultWebProxy = null;
                    webclient.Proxy = null;
                }

                return webclient;
            }
        }

        public RankingData GetRanking(string player)
        {
            return DownloadRankingData("/competitions/warlight-ai-challenge-2/leaderboard/global/a/", player);
        }

        public IEnumerable<RankingData> GetRankingsInInstitute(string institute)
        {
            return DownloadRankingsInInstitute(String.Format("/competitions/warlight-ai-challenge-2/leaderboard/institute/{0}/", institute));
        }

        private IEnumerable<RankingData> DownloadRankingsInInstitute(string url)
        {
            try
            {
                var dataList = new List<RankingData>();

                var htmlBytes = WebClient.DownloadData(Hostname + url);
                var html = System.Text.Encoding.UTF8.GetString(htmlBytes);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var leaderbord = doc.DocumentNode
                    .SelectNodes("//table")
                    .First(section => section.Attributes.Any(attribute => attribute.Value == "leaderboard-table"));

                var players = leaderbord.ChildNodes.First().ChildNodes;

                foreach (var player in players)
                {
                    var data = new RankingData();

                    data.Players = players.Count;
                    data.Name = player.SelectSingleNode(".//div[@class='user-name']").InnerText.Trim();
                    data.Bot = player.SelectSingleNode(".//div[@class='bot-name']").InnerText.Trim();
                    data.BotRevision = player.SelectSingleNode(".//div[@class='bot-revision']").InnerText.Trim();
                    data.Rank = player.SelectSingleNode(".//td[@class='cell-table cell-table-round cell-table-rank']").InnerText.Trim();
                    data.Score = player.SelectSingleNode(".//td[@class='cell-table cell-table-square']").SelectSingleNode(".//em").InnerText.Trim();

                    dataList.Add(data);
                }

                return dataList;
            }
            catch (IOException e)
            {
                return new List<RankingData>();
            }
        }

        private RankingData DownloadRankingData(string url, string playerName)
        {
            try
            {
                var data = new RankingData();

                var htmlBytes = WebClient.DownloadData(Hostname + url);
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
