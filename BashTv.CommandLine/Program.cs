using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BashTv.CommandLine
{
    class Program
    {
        private const string EpgRequestUrlFormat = "http://www.br.de/mediathek/video/programm/mediathek-programm-100~hal_date-{0}_vlt-epg_vt-medcc1_-83b61b034ab981e93a40b0f0057b7528f706f848.json";

        static void Main(string[] args)
        {
            var epgRequestUrl = string.Format(EpgRequestUrlFormat, "2015-12-21");
            var client = new WebClient();
            var json = client.DownloadString(epgRequestUrl);
            var epg = JObject.Parse(json);

            var items = new JArray();
            var brChannel = epg["channels"]["channel_28107"];
            var channelTitle = brChannel["channelTitle"].ToString();
            var channelBroadcasts = brChannel["broadcasts"];
            var unixEpochTime = new DateTime(1970, 1, 1);
            foreach (var broadcast in channelBroadcasts)
            {
                var headline = broadcast["headline"].ToString();
                var subTitle = broadcast["subTitle"].ToString();
                var broadcastStartDate = DateTime.Parse(broadcast["broadcastStartDate"].ToString());

                var item = new JObject();
                item["channelTitle"] = channelTitle;
                item["broadcastStartDate"] = broadcastStartDate.Subtract(unixEpochTime).TotalSeconds;
                item["headline"] = headline;
                item["subTitle"] = subTitle;

                items.Add(item);
            }

            Console.WriteLine(items.ToString());
            Console.ReadKey();
        }
    }
}
