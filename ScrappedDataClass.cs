using System.Collections.Generic;

namespace DSAEHonoursGUI
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
        
        // May be worth parsing the scrapped date data and changing it to a date time object
        public string PublishedDate { get; private set; }
        /// <summary>
        /// Text taken from the body of the news article
        /// </summary>
        public string ArticleText { get; }
        /// <summary>
        /// A collection of quote objects found within the body of the parsed text
        /// </summary>
        private List<Quote> FoundQuotes { get; }
        /// <summary>
        /// Add a list of quotes to the ParsedData object
        /// </summary>
        /// <param name="temp"></param>

        public ScrappedData(string title, string url, string rss, string author, string description, string text)
        {
            Title = title;
            URL = url;
            Author = author;
            Descript = description;
            ArticleText = text;
            RSS_Source = rss;
        }
        /// <summary>
        /// Method to add only a single found quote object
        /// </summary>
        /// <param name="single"></param>
        public void AddQuotes(Quote single)
        {
            FoundQuotes.Add(single);
        }
        /// <summary>
        /// Method to add multiple found quote objects
        /// </summary>
        /// <param name="quotes"></param>
        public void AddQuotes(List<Quote> quotes)
        {
            FoundQuotes.AddRange(quotes);
        }
        /// <summary>
        /// Method to add or change the publication date of an article in a ScrappedData object
        /// </summary>
        /// <param name="date"></param>
        public void SetPublishedDate(string date, string source)
        {
            if (source == null)
            {
                PublishedDate = "unknown";
            }
            // maybe use a switch statement instead
            else if(source.Contains("News24") || source.Contains("IOL"))
            {
                PublishedDate = date.Substring(0, 10);
                // Format: 2021-02-06T07:06:40.487Z
                // Grab everything from the start and until the T exclusive
            } 
            else if (source.Contains("Eyewitness"))
            {
                PublishedDate = date;
                // format: yyyy-mm-dd
            }
            else if (source.Contains("BusinessLive") || source.Contains("TimesLive") || source.Contains("SowetanLIVE"))
            {
                PublishedDate = date.Substring(0, date.IndexOf('-') - 1);
                // format: 06 February 2021 - 09:07
                // Maybe convert to dateTime to get dd/mm/yyyy
            }
        }
        /// <summary>
        /// Overridden to string method
        /// </summary>
        /// <returns>Prints each field on a new line</returns>
        public override string ToString()
        {
            return $"URL: {URL},\n Title: {Title}\nAuthor: {Author}\nDescription: {Descript},\nPublished Date: {PublishedDate}\n";
        }
    }
}
