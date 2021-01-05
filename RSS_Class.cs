using CodeHollow.FeedReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class RSS_Class
{
    /// <summary>
    /// List of SA specific feeds from various news sites
    /// </summary>
    private List<string> SA_RSS_Feeds { get; set; }
    private int NumFeeds { get; set; }


    public RSS_Class(string filepath)
    {
        readRSSFile(filepath);
    }

    public void readRSSFile(string filepath)
    {
        if (!(File.Exists(filepath)))
        {
            throw new Exception("RSS Feed file cannot be found at this path: " + filepath);
        }
        else
        {
            SA_RSS_Feeds = File.ReadLines(filepath)
                .Where(line => !line.Contains('#'))
                // Use a hash to skip lines in the file
                .Where(line => !string.IsNullOrEmpty(line)).ToList();
        }
    } 

    public void addRSSfeedtoFile(string filepath, List<string> rssList)
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

            readRSSFile(filepath);
            // Calls function to update the list of RSS feeds
        }
    }
    /// <summary>
    /// Opens SaRssFeed for reading and Async retrieves the list of stored news page URLs
    /// Returns the URL and the title of the RSS feed as a source to enable parsing 
    /// </summary>
    /// <param name="DoArchive">Bool to decide if the XML RSS feeds should be saved locally</param>
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
                    //Console.WriteLine($"Read {++count} from RSS Feed: {feed.Result.Title}");
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
