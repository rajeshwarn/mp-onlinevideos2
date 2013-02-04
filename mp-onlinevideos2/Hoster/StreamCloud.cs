﻿using System;
using System.Net;
using System.Text.RegularExpressions;

using OnlineVideos.Hoster.Base;
using OnlineVideos.Sites;


namespace OnlineVideos.Hoster
{
    public class StreamCloud : HosterBase
    {
        public override string getHosterUrl()
        {
            return "streamcloud.eu";
        }

        private string requestFileInformation(string url, CookieContainer cc)
        {
            string webData = SiteUtilBase.GetWebData(url, cc);
            if (!string.IsNullOrEmpty(webData))
            {
                if (!string.IsNullOrEmpty(getRegExData(@"(?<exists>This\sfile\sdoesn\'t\sexist,\sor\shas\sbeen\sremoved\s?\.)", webData, "exists")))
                    webData = string.Empty;
            }
            return webData;
        }

        public override string getVideoUrls(string url)
        {
            //Get HTML from url
            CookieContainer cc = new CookieContainer();
            string webData = requestFileInformation(url, cc);
            if (string.IsNullOrEmpty(webData)) return string.Empty;

            if (!string.IsNullOrEmpty(webData))
            {
                //Extract op value from HTML form
                string op = Regex.Match(webData, @"op""\svalue=""(?<value>[^""]*)").Groups["value"].Value;
                //Extract id value from HTML form
                string id = Regex.Match(webData, @"id""\svalue=""(?<value>[^""]*)").Groups["value"].Value;
                //Extract fname value from HTML form
                string fname = Regex.Match(webData, @"fname""\svalue=""(?<value>[^""]*)").Groups["value"].Value;                
                //Extract hash value from HTML form
                string hash = Regex.Match(webData, @"hash""\svalue=""(?<value>[^""]*)").Groups["value"].Value;
                //Extract hash value from HTML form
                string imhuman = Regex.Match(webData, @"imhuman"".*?value=""(?<value>[^""]*)").Groups["value"].Value;

                //Wait for 10 seconds
                System.Threading.Thread.Sleep(10000);

                //Send Postdata (simulates a button click)
                string postData = @"op=" + op + "&usr_login=&id=" + id + "&fname=" + fname + "&referer=&hash=" + hash + "&imhuman=" + imhuman;
                string webData2 = GenericSiteUtil.GetWebDataFromPost(url, postData, cc);

                //Extract file url from HTML
                Match n = Regex.Match(webData2, @"file: ""(?<url>[^""]*)");
                if (n.Success)
                {
                    videoType = VideoType.flv;
                    return n.Groups["url"].Value;
                }
            }
            return String.Empty;
        }
    }
}