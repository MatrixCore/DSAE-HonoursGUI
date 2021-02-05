using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            ContextSentences = cs;
            PublishedDate = date;
            AuthorName = author;
            SourceTitle = title;
            SourceType = type;
        }
        override public string ToString()
        {
            return $"{SearchWord}: {string.Join("\n", ContextSentences) } " +
                $"\n{AuthorName}, {SourceTitle}, {SourceType}" +
                $"{PublishedDate}";
        }

        public static void OutputXML(string filePath, List<Quote> quotes)
        {
            if (!File.Exists(filePath))
            { // load xml file
                Console.WriteLine("File cannout be found at " + filePath);
            }
            else
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true
                };

                XmlWriter writer = XmlWriter.Create(filePath + "DSAEoutput.xml", settings);
                writer.WriteStartDocument();
                // Check how to open an XML file

                quotes.Select(q =>
                // for each quote object, convert to XML and append to the XML 
                {
                    writer.WriteStartElement("IntakeRec");

                    writer.WriteStartElement("QuotationYear");
                    writer.WriteString("2020");
                    writer.WriteEndElement();

                    writer.WriteStartElement("PublicationDate");
                    writer.WriteString(q.PublishedDate);
                    writer.WriteEndElement();

                    writer.WriteStartElement("AuthorInfo");
                    writer.WriteString(q.AuthorName);
                    writer.WriteEndElement();

                    writer.WriteStartElement("PrintSource");
                    writer.WriteString(q.SourceTitle);
                    writer.WriteEndElement();

                    writer.WriteStartElement("ElectronicSource");
                    writer.WriteString("SA Online Newspaper");
                    writer.WriteEndElement();

                    writer.WriteStartElement("LexicalInfo");
                    // Add in a loop to print every excerpt per quote article
                    q.ContextSentences.ForEach(sentence =>
                        {
                            writer.WriteStartElement("Excerpt");
                            writer.WriteString(sentence);
                            writer.WriteEndElement();
                        });
                    writer.WriteEndElement();

                    writer.WriteStartElement("FinalReviewComment");
                    writer.WriteString(" ");
                    writer.WriteEndElement();
                    return 0;
                });
                writer.WriteEndElement();
                // closes the IntakeRec tag
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
            }
        }
    }

}
