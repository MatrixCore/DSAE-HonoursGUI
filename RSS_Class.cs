using CodeHollow.FeedReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DSAEHonoursGUI
{
    public class RSS_Class
    {
        /// <summary>
        /// List of SA specific feeds from various news sites
        /// </summary>
        private List<string> SA_RSS_Feeds { get; set; }
        /// <summary>
        /// Number of RSS Feed links current in the list
        /// </summary>
        private int NumFeeds { get; set; }


        public RSS_Class(string filepath)
        {
            ReadRSSFile(filepath);
        }
        /// <summary>
        /// Reads in the RSS text file and adds the urls to the RSS_Feeds list object
        /// </summary>
        /// <param name="filepath"></param>
        public void ReadRSSFile(string filepath)
        {
            if (!(File.Exists(filepath)))
            {
                throw new Exception("RSS Feed file cannot be found at this path: " + filepath);
            }
            else
            {
                SA_RSS_Feeds =
                File.ReadLines(filepath)
                .Where(line => !line.Contains('#'))
                // Use a hash to skip lines in the file
                .Where(line => !string.IsNullOrEmpty(line))
                .ToList();
            }
        }

        public void AddRSSfeedtoFile(string filepath, List<string> rssList)
        {
            if (!(File.Exists(filepath)))
            {
                throw new Exception("RSS Feed file cannot be found at this path: " + filepath);
            }
            else
            {
                var feeds = File.ReadLines(filepath);
                File.WriteAllLines(filepath, rssList.Where(line => !feeds.Contains(line)));
                // Filters out all the rss feeds that are already in the rss file and writes the ones that aren't 

                ReadRSSFile(filepath);
                // Calls function to update the list of RSS feeds
            }
        }
        /// <summary>
        /// Opens SA_RSS_Feeds for reading and async retrieves the list of stored news page URLs
        /// Returns the URL and the title of the RSS feed as a source to enable parsing 
        /// </summary>
        /// <returns>Collection of string URLs</returns>
        public IEnumerable<Tuple<string, string>> LoadRSS()
        {
            //int count = 0;
            try
            {
                return
                SA_RSS_Feeds.AsParallel()
                .Select(rss => FeedReader.ReadAsync(rss))
                .SelectMany(feed =>
                {
                    return feed.Result.Items
                    .Where(item => item != null)
                    .Distinct()
                    .Select(item =>
                    {
                        //return (link: item.Link, title: feed.Result.Title); - would prefer to have the tuple values have names attached
                        return Tuple.Create(item.Link, feed.Result.Title);
                    });
                });
            }
            catch (Exception e)
            {
                Console.WriteLine("RSS parser failed with this error: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Returns the number of feeds
        /// </summary>
        /// <returns></returns>
        public int GetNumRSSfeeds()
        {
            return NumFeeds;
        }
    }
}
