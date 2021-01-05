using System.Xml;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace DSAEHonours
{
    public class Quote
    // Bibliographical info is required for each quotation.
    // Essential: year, source title, source type(here monograph or book source).
    // Other details are nice to have: author, translator etc.
    {
        /// <summary>
        /// The associated sentences of the found quote
        /// </summary>
        private List<string> contextSentences { get; }
        /// <summary>
        /// The keyword that flagged the quote for collection
        /// </summary>
        private string SearchWord { get; }
        /// <summary>
        /// 
        /// </summary>
        private string PublishedDate { get; }
        /// <summary>
        /// 
        /// </summary>
        private string AuthorName { get; }
        /// <summary>
        /// 
        /// </summary>
        private string SourceTitle { get; }
        private string SourceType { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="search"></param>
        /// <param name="text"></param>
        /// <param name="date"></param>
        /// <param name="author"></param>
        /// <param name="title"></param>
        /// <param name="type"></param>

        public Quote(string search, List<string> cs, string date, string author, string title, string type)
        {
            SearchWord = search;
            contextSentences = cs;
            PublishedDate = date;
            AuthorName = author;
            SourceTitle = title;
            SourceType = type;
        }
        override public string ToString()
        {
            return $"{SearchWord}: {string.Join('\n', contextSentences) } " +
                $"\n{AuthorName}, {SourceTitle}, {SourceType}" +
                $"{PublishedDate}";
        }

        public static XDocument OutputXML(string filePath, List<Quote> quotes)
        {
            var xDoc = new XDocument();
            if (File.Exists(filePath)) { xDoc = XDocument.Load(filePath); }
            
            quotes.AsParallel().Select(q =>
            // for each quote object, convert to XML and append to the XML 
            {
                xDoc.Add(
                    new XElement("IntakeRec",
                        new XElement("BiblioInfo",
                            new XElement("QuotationYear", 2020),
                            new XElement("PublicationDate", q.PublishedDate),
                            new XElement("AuthorInfo", q.AuthorName),
                            new XElement("PrintSource", q.SourceTitle),
                            new XElement("ElectronicSource", "Online Newspaper")
                            ),

                        new XElement("Lexicalinfo",
                            new XElement("Excerpt")),

                        new XElement("FinalReviewComment")));

                return 0;
            });
            xDoc.Declaration = new XDeclaration("1.0", "utf-8", "true");
            
            return xDoc;
            // change to void
            // Write to file
        }
    }

}
