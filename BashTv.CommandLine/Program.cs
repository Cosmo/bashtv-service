using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

using Newtonsoft.Json.Linq;

namespace BashTv.CommandLine
{
    class Program
    {
        private const string GuideRequestUrl = "http://www.br.de/mediathek/video/programm/mediathek-programm-100~hal_vt-medcc1_-bff08c03fe069a9ee9013704adcbd4855992ad2a.json";

        static void Main(string[] args)
        {
            var client = new WebClient();
            var guideJson = client.DownloadString(GuideRequestUrl);
            var guide = JObject.Parse(guideJson);
            var epgDayLinks = guide["epgDays"]["_links"];
            var tidyGuides = new Dictionary<string, JArray>();
            var channelIdNameMap = new Dictionary<string, string>
            {
                { "channel_28107", "br" },
                { "channel_28487", "ard-alpha" }
            };

            foreach (var link in epgDayLinks.Children().Take(10))
            {
                var href = link.First["href"].ToString();
                Console.WriteLine("Fetching '" +  href + "'");

                var epgJson = client.DownloadString(href);
                var epg = JObject.Parse(epgJson);
                var items = new JArray();
                var channels = epg["channels"];

                foreach (var channel in channels)
                {
                    var channelId = ((JProperty)channel).Name;
                    var channelProperties = ((JProperty)channel).Value;
                    var channelTitle = channelProperties["channelTitle"].ToString();
                    var channelBroadcasts = channelProperties["broadcasts"];
                    var unixEpochTime = new DateTime(1970, 1, 1);
                    foreach (var broadcast in channelBroadcasts)
                    {
                        var headline = broadcast["headline"].ToString();
                        var subTitle = broadcast["subTitle"].ToString();
                        var broadcastStartDate = DateTime.Parse(broadcast["broadcastStartDate"].ToString());
                        var broadcastEndDate = DateTime.Parse(broadcast["broadcastEndDate"].ToString());
                        var duration = broadcastEndDate - broadcastStartDate;

                        var broadcastLinks = broadcast["_links"];
                        if (broadcastLinks == null) continue;

                        var livestream = broadcastLinks["livestream"];
                        var livestreamHref = livestream["href"].ToString();
                        var livestreamJson = client.DownloadString(livestreamHref);
                        var livestreams = JObject.Parse(livestreamJson);
                        var hlsLivestream = livestreams["assets"].FirstOrDefault(x => x["type"].ToString() == "HLS");
                        if (hlsLivestream == null) continue;
                        var hlsLivestreamHref = hlsLivestream["_links"]["stream"]["href"].ToString();

                        var item = new JObject();
                        item["channel"] = channelTitle;
                        item["start"] = (int)broadcastStartDate.Subtract(unixEpochTime).TotalSeconds;
                        item["end"] = (int)broadcastEndDate.Subtract(unixEpochTime).TotalSeconds;
                        item["duration"] = (int) duration.TotalSeconds;
                        item["title"] = headline;
                        item["episode"] = subTitle;
                        item["streamUrl"] = hlsLivestreamHref;
                        items.Add(item);
                    }

                    var readableChannelName = channelIdNameMap[channelId];
                    if (tidyGuides.ContainsKey(readableChannelName))
                    {
                        var existingItems = tidyGuides[readableChannelName];
                        foreach (var item in items)
                        {
                            existingItems.Add(item);
                        }
                    }
                    else
                        tidyGuides.Add(readableChannelName, items);
                }
            }

            foreach (var tidyGuide in tidyGuides)
            {
                File.WriteAllText(tidyGuide.Key + ".json", tidyGuide.Value.ToString());
            }
        }
    }
}
