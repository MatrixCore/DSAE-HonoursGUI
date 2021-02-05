using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Windows;
using System.Reflection;

namespace DSAEHonoursGUI
{
    public class Main
    {
        static public void main()
        {
            ProcessRSSfeeds();
        }

        static void ProcessRSSfeeds()
        {
            var urlList = new RSS_Class(Directory.GetCurrentDirectory() + @"\files\RSS_Feeds.txt")
                    .LoadRSS()
                    .Where(feed => feed != null)
                    .ToList();

            Console.WriteLine($"{urlList.Count} URLs from RSS feeds");
            var Searchlist = new SearchWordList(Directory.GetCurrentDirectory() + @"\files\dsae-dictionary-search-list-2020-A-Z.utf8.txt");

            if (!Searchlist.IsEmpty())
            {
                var quoteList = 
                    HTMLScraper.ProcessUrls(urlList).ToList()
                    .Where(item => item != null)
                    .AsParallel()
                    .AsOrdered()
                    .SelectMany(data => HTMLScraper.FindSearchWords(data, Searchlist, 0))
                    .ToList();

                Quote.OutputXML(Directory.GetCurrentDirectory() + @"\files\Output\DSAEoutput.xml", quoteList);

                Console.WriteLine($"Found number of words: {quoteList.Count()}");
            };

            Console.WriteLine("\nGREAT SUCCESS");
        }
    }
}
