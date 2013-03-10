using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using CommandLine;
using CommandLine.Text;

namespace YahooGroupMemberScraper
{
    class Program
    {
        class Options
        {
            [Option('l', "login", Required = true,
              HelpText = "Yahoo! login.")]
            public string Login { get; set; }

            [Option('p', "password", Required = true,
            HelpText = "Yahoo! password.")]
            public string Password { get; set; }

            [Option('g', "groupname", DefaultValue = "VasectomyPain",
            HelpText = "Yahoo! password.")]
            public string GroupName { get; set; }

            [Option('o', "output", DefaultValue = "profiles.txt",
            HelpText = "Yahoo! password.")]
            public string OutputFile { get; set; }

            [Option('i', "interval", DefaultValue = 2000,
            HelpText = "Interval between requests (in milliseconds).")]
            public int Interval { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }


        
        /// <summary>
        /// Gets a list of all members subscribed to a Yahoo group.
        /// </summary>
        static void Main(string[] args)
        {

            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
                return;

            CookieContainer _yahooContainer;

            string strPostData = String.Format("login={0}&passwd={1}", options.Login, options.Password);

            // Setup the http request.
            HttpWebRequest wrWebRequest = WebRequest.Create("http://login.yahoo.com/config/login") as HttpWebRequest;
            wrWebRequest.Method = "POST";
            wrWebRequest.ContentLength = strPostData.Length;
            wrWebRequest.ContentType = "application/x-www-form-urlencoded";
            _yahooContainer = new CookieContainer();
            wrWebRequest.CookieContainer = _yahooContainer;

            // Post to the login form
            using (StreamWriter swRequestWriter = new StreamWriter(wrWebRequest.GetRequestStream()))
            {
                swRequestWriter.Write(strPostData);
                swRequestWriter.Close();
            }

            // Get the response from the login post
            HttpWebResponse hwrWebResponse = (HttpWebResponse)wrWebRequest.GetResponse();
            if (!hwrWebResponse.ResponseUri.AbsoluteUri.Contains("my.yahoo.com"))
            {
                Console.WriteLine("Login failed.");
                return;
            }

            int startPos = 1;
            ArrayList profileList = new ArrayList();

            while (true)
            {
                // Pull down a member list page
                string url = String.Format("http://health.groups.yahoo.com/group/{0}/members?start={1}&group=sub", options.GroupName, startPos);
                HttpWebRequest req = (HttpWebRequest) WebRequest.Create(url);
                req.CookieContainer = _yahooContainer;
                HttpWebResponse resp = (HttpWebResponse) req.GetResponse();

                String responseString;
                using (Stream stream = resp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    responseString = reader.ReadToEnd();
                }

                
                /*  Find the profile names in the HTML.  We're looking for something like this:
                  
                    <td class="yid selected ygrp-nowrap">
                    <a href="http://profiles.yahoo.com/Faith_50">Faith_50</a> </td>
                    <td class="email ygrp-nowrap">
                    <a href="/group/VasectomyPain/post?postID=p266YGV28ctpvZQ-NTeOMXEZmWEx-o7PlEK11MR2eTnJi--HM4quWR0pAB9xZ2T20zcjTSumoxo">faith_50@...</a>
                    </td>
                 
                 */

                const string pattern = @"<td class=""yid selected ygrp-nowrap"">\n<a href=""http://profiles.yahoo.com/(.*?)"".*>.*?</a> </td>\n<td class=""email ygrp-nowrap"">\n<a href=""/group/VasectomyPain/post\?postID=.*?"">(.*?)@";

                Match m = Regex.Match(responseString, pattern);

                if (m.Length == 0)
                {
                    Console.WriteLine("Error. No matches found on page. Waiting 60 seconds before retrying...");
                    Thread.Sleep(60000);
                    continue;
                }

                while (m.Success)
                {
                    // put together the profile name and partial email address
                    string profileAndPartialEmail = Uri.UnescapeDataString(m.Groups[1].Value + "," + m.Groups[2].Value);

                    if (!profileList.Contains(profileAndPartialEmail))
                    {
                        profileList.Add(profileAndPartialEmail);
                        Console.WriteLine(profileAndPartialEmail);
                    }
                    m = m.NextMatch();
                }

                // Find the URL for the 'Next >' link to get the next startPos...
                string nextPagePattern = @"<a href=""/group/VasectomyPain/members\?start=([0-9]*)&amp;group=sub"">Next&nbsp;&gt;</a>";
                Match matchNextPage = Regex.Match(responseString, nextPagePattern);
                if (!matchNextPage.Success)
                {
                    // we've reached the end.  Let's write out our file.
                    WriteListToFile(profileList, options.OutputFile);

                    Console.WriteLine("Done. Press any key to continue");
                    Console.ReadKey();
                    break;
                }

                // bump up the start pos
                startPos = Int32.Parse(matchNextPage.Groups[1].Value);

                // Wait for some time so we don't look like a DOS attack
                Thread.Sleep(options.Interval);

            }
        }

        private static void WriteListToFile(ArrayList list, string filename)
        {

            StreamWriter output = new StreamWriter(filename);
            foreach (string s in list)
                output.WriteLine(s);

            output.Close();

        }
    }
}
