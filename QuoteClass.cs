using System;
using System.Collections.Generic;
using System.Xml;

namespace DSAEHonoursGUI
{
    public class Quote
    // Bibliographical info is required for each quotation.
    // Essential: year, source title, source type (here monograph or book source).
    // Other details are nice to have: author, translator etc.
    {
        /// <summary>
        /// The associated sentences of the found quote in the same article
        /// </summary>
        private List<string> ContextSentences { get; }
        /// <summary>
        /// The keyword that flagged the quote for collection
        /// </summary>
        private string SearchWord { get; }
        /// <summary>
        /// Date of published article from the meta data of the article
        /// </summary>
        private string PublishedDate { get; }
        /// <summary>
        /// Name of the author of the news article
        /// </summary>
        private string AuthorName { get; }
        /// <summary>
        /// Title of the news article
        /// </summary>
        private string SourceTitle { get; }
        /// <summary>
        /// From which RSS feed this article was found
        /// </summary>
        private string RSSsource { get; }
        /// <summary>
        /// Link to the original article from where quote orignates
        /// </summary>
        private string URLsource { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="search"></param>
        /// <param name="text"></param>
        /// <param name="date"></param>
        /// <param name="author"></param>
        /// <param name="title"></param>
        /// <param name="type"></param>

        public Quote(string search, List<string> cs, string date, string author, string title, string rss, string url)
        {
            SearchWord = search;
            ContextSentences = cs;
            PublishedDate = date;
            AuthorName = author;
            SourceTitle = title;
            RSSsource = rss;
            URLsource = url;
        }
        override public string ToString()
        {
            return $"{SearchWord}: {string.Join("\n", ContextSentences) } " +
                $"\n{AuthorName}, {SourceTitle}, {RSSsource}" +
                $"{PublishedDate}";
        }

        public static void OutputXML(string filePath, List<Quote> quotes)
        {
            // change to generate a new xml file with the date as a file name
            // Remember to check if the file for today already exists

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true
            };

            XmlWriter writer = XmlWriter.Create(filePath, settings);
            writer.WriteStartDocument();

            writer.WriteStartElement("IntakeRec");
            //Top level tag
            foreach (Quote q in quotes)
            // for each quote object, convert to XML
            {
                writer.WriteStartElement("Quote");

                writer.WriteStartElement("BibloInfo");

                writer.WriteStartElement("QuotationYear");
                writer.WriteString($"{DateTime.Today.Year}");
                writer.WriteEndElement();
                //closes QuotationYear

                writer.WriteStartElement("PublicationDate");
                writer.WriteString(q.PublishedDate == null ? "Unknown" : q.PublishedDate);
                writer.WriteEndElement();
                //closes PublicationDate

                writer.WriteStartElement("AuthorInfo");
                writer.WriteString(q.AuthorName == null ? "Unknown" : q.AuthorName);
                writer.WriteEndElement();
                //closes AuthorInfo

                writer.WriteStartElement("PrintSource");
                writer.WriteString(q.SourceTitle == null ? "Unknown" : q.SourceTitle);
                writer.WriteEndElement();
                //closes PrintSource

                writer.WriteStartElement("ElectronicSource");
                writer.WriteString(q.RSSsource);
                writer.WriteEndElement();
                //closes ElectronicSource

                writer.WriteStartElement("URLsource");
                // Doesn't conform to schema, may need to be removed
                writer.WriteString(q.URLsource);
                writer.WriteEndElement();
                //closes URLsource

                writer.WriteEndElement();
                //closes BibloInfo              

                writer.WriteStartElement("LexicalInfo");

                writer.WriteStartElement("CatchWord");
                writer.WriteString(q.SearchWord);
                writer.WriteEndElement();
                //closes CatchWord    

                foreach (string context in q.ContextSentences)
                {
                    writer.WriteStartElement("Excerpt");
                    writer.WriteString(context);
                    writer.WriteEndElement();
                    //closes Excerpt
                };
                writer.WriteEndElement();
                //closes LexicalInfo

                writer.WriteStartElement("FinalReviewComment");
                // Not if this is needed
                writer.WriteString(" ");
                writer.WriteEndElement();
                //closes FinalReviewComment

                writer.WriteEndElement();
                //closes Quote
            };
            writer.WriteEndElement();
            //closes top level tag
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }

    }

}
