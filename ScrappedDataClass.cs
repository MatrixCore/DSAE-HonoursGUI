using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;

namespace DSAEHonours
{
    /// <summary>
    ///    Class to hold data scrapped from web pages
    /// </summary>
    public class ScrappedData // HTML Meta tags - https://gist.github.com/whitingx/3840905
    // May be expanded to also contain DSAE catchwords
    {
        /// <summary>
        /// The title of the web page
        /// </summary>
        public string Title { get; private set; }
        /// <summary>
        /// Canonical URL for web page
        /// </summary>
        public string URL { get; private set; }
        /// <summary>
        /// RSS Feed from where the page was retrieved
        /// </summary>
        public string RSS_Source { get; private set; }
        /// <summary>
        /// Author name of web page
        /// </summary>
        public string Author { get; private set; }
        /// <summary>
        /// Description of the web page
        /// </summary>
        public string Descript { get; private set; }
        /// <summary>
        /// Date of web page publication
        /// </summary>
        public string PublishedDate { get; private set; }
        /// <summary>
        /// Text taken from the body of the news article
        /// </summary>
        public string articleBody { get; }
        /// <summary>
        /// Save location of the original HTML document
        /// </summary>
        public string HTMLsavePath { get; }
        /// <summary>
        /// A collection of quote objects found within the body of the parsed text
        /// </summary>
        private List<Quote> FoundQuotes { get; }
        /// <summary>
        /// Add a list of quotes to the ParsedData object
        /// </summary>
        /// <param name="temp"></param>

        public ScrappedData(string title, string url, string rss, string author, string description, string body, string savepath)
        {
            Title = title;
            URL = url;
            Author = author;
            Descript = description;
            articleBody = body;
            HTMLsavePath = savepath;
            RSS_Source = rss;
        }

        public void addQuotes(Quote single)
        {
            FoundQuotes.Add(single);
        }

        public void addQuotes(List<Quote> quotes)
        {
            FoundQuotes.AddRange(quotes);
        }

        public void setPublishedDate(string date)
        {
            PublishedDate = date;
        }
        /// <summary>
        /// Need I say more?
        /// </summary>
        /// <returns>Wanna guess?</returns>
        public override string ToString()
        {
            return $"URL: {URL},\n Title: {Title}\nAuthor: {Author}\nDescription: {Descript},\nPublished Date: {PublishedDate}\n";
        }

        public HtmlDocument Loadhtml(HtmlDocument doc)
        {
            try
            {
                doc.LoadHtml(File.ReadAllText(HTMLsavePath));
                return doc;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
