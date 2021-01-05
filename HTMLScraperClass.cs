using HtmlAgilityPack;
using Phonix;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

//Legend:
//	LZ - Leipzig data
//	HH - Hildesheim data
/// <summary>
/// Namespace for the entire project
/// </summary>
namespace DSAEHonours
{
    public class HTMLScraper
    {
        public static Regex urlPattern = new Regex(@"(?<host>https?:\/\/www\.(?<domain>[-a-zA-Z0-9@:%._\+~#=]{1,256})\.[a-zA-Z0-9()]{1,6}\b)([-a-zA-Z0-9()@:%_\+.~#?&\/\/=]*)");
        // Credits to https://stackoverflow.com/a/3809435
        // Added host anchor

        /// <summary>
        /// Accepts a list of URLS and returns a list of ParsedData (extracted metadata) objects
        /// </summary>
        /// <param name="URLs"></param>
        /// <returns></returns>
        public static IEnumerable<ScrappedData> processUrls(IEnumerable<Tuple<string, string>> URLs, bool ArchivePage = false)
        {
            // Poential change: Don't save the whole document, rather use getTextNodes() to grab the article body and drop the request of the HTML document as nothing else is being used 
            // Still archive the HTML document but only keep the article body in memory

            //int count = 0;
            return URLs.AsParallel().Select(address =>
            {
                try
                {

                    var web = new HtmlWeb();
                    var html = web.Load(address.Item1);
                    if (web.StatusCode != HttpStatusCode.OK) { throw new Exception($"Web status (processUrls): {web.StatusCode}"); }
                    // Check for 302 or 500 code
                    // Gets last HTTP status code and throws exception if not == OK (200)
                    var text = GetTextNodes(html, address.Item2);
                    if (ArchivePage)
                    {
                        var workDir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.FullName;
                        string currDir = workDir + @"\output\HTMLpages\" + urlPattern.Match(address.Item1).Groups["domain"].Value;
                        // extracts the domain of each url to group the sources 
                        Directory.CreateDirectory(currDir);
                        var numFiles = Directory.GetFiles(currDir).Length;
                        string savepath = workDir + $@"\output\HTMLpages\{urlPattern.Match(address.Item1).Groups["domain"].Value}\html{numFiles + 1}.html";

                        // URL to File Name table 
                        var writer = File.AppendText(workDir + @"\output\HTMLpages\HTMLhashtable.txt");
                        writer.WriteLine(address.Item1, $@"{urlPattern.Match(address.Item1).Groups["domain"].Value}\html{ numFiles + 1}");
                        writer.Close();

                        File.WriteAllText(savepath, html.Text);
                        return new { Doc = html, Url = address, SavePath = savepath, body = text };
                    }
                    else
                    {
                        return new { Doc = html, Url = address, SavePath = "", body = text };
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"(processUrls) {address}: {e.Message}");
                    return null;
                }
            })
            .Select(data =>
            {
                if (data == null) { return null; }
                //Console.WriteLine($"Read {++count} url: {data.Url.Item1}");
                return getPublishedDate(data.Doc, data.Url.Item2, new ScrappedData(
                    // Search through the head tag to find meta data
                    title: data?.Doc?.DocumentNode?.SelectSingleNode("//head/title")?.InnerText,
                    author: data?.Doc?.DocumentNode?.SelectSingleNode("//head/meta[@name='author']")?.Attributes["content"]?.Value,
                    description: data?.Doc?.DocumentNode?.SelectSingleNode("//head/meta[@name='description']")?.Attributes["content"]?.Value,
                    body: data?.body,
                    url: data?.Url.Item1,
                    savepath: data?.SavePath == "" ? null : data?.SavePath,
                    // Checks if there is a SavePath in the tuple, otherwise it is nulled
                    rss: data?.Url.Item2
                    ));
            });
        }      

        /// <summary>
        /// Due to the different ways web pages store their publication date, this message needs to pull that info depending on which publciation the HTML comes from
        /// </summary>
        /// <param name="incomplete"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        private static ScrappedData getPublishedDate(HtmlDocument Doc, string source, ScrappedData incomplete)
        {
            try
            {
                if (source.Contains("News24"))
                {
                    incomplete.setPublishedDate(Doc?.DocumentNode?.SelectSingleNode("//head/meta[@name='publisheddate']")?.Attributes["content"]?.Value);
                }
                else if (source.Contains("Eyewitness News") || source.Contains("IOL"))
                {
                    incomplete.setPublishedDate(Doc?.DocumentNode?.SelectSingleNode("//meta[@itemprop = 'datePublished']")?.Attributes["content"]?.Value);
                }
                else if (source.Contains("BusinessLIVE") || source.Contains("TimesLIVE"))
                {
                    incomplete.setPublishedDate(Doc.DocumentNode.SelectSingleNode("//div[@class = 'article-pub-date ']").Attributes["content"].Value); // Not working for either
                }
                else if (source.Contains("SowetanLIVE"))
                {
                    incomplete.setPublishedDate(Doc.DocumentNode.SelectSingleNode("//span[@class = 'article-pub-date']").Attributes["content"].Value); // Not working
                }
                else
                {
                    incomplete.setPublishedDate("Unknown");
                }

                return incomplete;

            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                incomplete.setPublishedDate("Unknown");
                return incomplete;
            }
        }

        /// <summary>
        /// Returns all the text found in the article body of a single News Story
        /// </summary>
        /// <param name="DocNode"></param>
        /// <returns></returns>
        public static string GetTextNodes(HtmlDocument doc, string RSSsource)
        {
            // EWN -> <span itemprop="articleBody">                             RSS Feed title = "Eyewitness News | Latest News ( Local )"
            // News24 -> <div class="article__body">                            RSS Feed title = "News24 South Africa"
            // BusinessLive -> <div class=wrap>  <div class=text>               RSS Feed title = "BusinessLIVE > news ".trim()
            // IOL -> <div itemProp="articleBody">                              Rss Feed title = "IOL section feed for South Africa"
            // TimesLive -> hot mess
            try
            {
                if (RSSsource.Contains("Eyewitness"))
                {
                    return doc.DocumentNode.SelectSingleNode("//span[@itemprop = 'articleBody']").InnerText;
                }
                else
                if (RSSsource.Contains("News24"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@class = 'article__body']").InnerText;
                }
                else
                if (RSSsource.Contains("BusinessLIVE"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@class = 'wrap']/div[@class = 'text']").InnerText;
                }

                else
                if (RSSsource.Contains("TimesLIVE"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@class = 'wrap']/div[@class = 'text']").InnerText;
                }
                else
                if (RSSsource.Contains("SowetanLIVE"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@class = 'wrap']/div[@class = 'text']").InnerText;
                }
                else
                if (RSSsource.Contains("IOL"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@itemprop = 'articleBody']").InnerText;
                }
                else
                if (RSSsource.Contains("Daily Maverick"))
                {
                    return doc.DocumentNode.SelectSingleNode("").InnerText;
                }
                else
                if (RSSsource.Contains("Politicsweb"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@class = 'article-container']").InnerText;
                }

                else { throw new Exception("Unrecognized RSS feed source"); }
            }
            catch (Exception e)
            {
                Console.WriteLine("(GetTextNodes) " + e.Message);
                return null;
            }
        }

        private static string SantizeText(string Splittext)
        {
            //cases like letter.letter or letter." or letter.'

            // OR HTML Ampersand Character Codes like &quot
            // Use String.replace() instead of Regex, less resource intesive

            return Splittext.Replace(",", " ");
        }

        /// <summary>
        /// 
        /// </summary>
        public static List<Quote> FindSearchWords(ScrappedData data, SearchWordList searchWordList, int mode)
        {
            var splitText = SantizeText(data.articleBody)
                     .Split(' ')
                     .Where(str => !string.IsNullOrWhiteSpace(str))
                     .Where(word => word.Length > 1)
                     .ToList();
            try
            {
                // TODO: Add logic to handle search words that a multiple words in length
                // Perhaps search by first word and then check if following word matches
                switch (mode)
                {
                    case 0:
                        return GenerateQuotes(StringMatchWords(splitText, searchWordList), data, splitText);

                    case 1:
                        return GenerateQuotes(SoundExMatchWords(splitText, searchWordList), data, splitText);

                    case 2:
                        return GenerateQuotes(MetaphoneMatchWords(splitText, searchWordList), data, splitText);

                    case 3:
                        return GenerateQuotes(EditDistMatchWords(splitText, searchWordList), data, splitText);

                    default:
                        throw new Exception("Invalid matching mode value. Please select {1,2,3,4}");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("(GetFoundWords) " + e.Message);
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="foundWords"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<Quote> GenerateQuotes(List<string> foundWords, ScrappedData data, List<string> splitText)
        {
            try
            {
                return foundWords.Select(word =>
                                new Quote(word,
                                FindQuoteSentences(splitText, word),
                                data.PublishedDate, data.Author, data.Title, data.RSS_Source))
                                .ToList();
            }
            catch(Exception e)
            {
                Console.WriteLine("(GenerateQuotes) " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Method to return the sentence the search word was found in by looking forwards and backwards for fullstops
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private static List<string> FindQuoteSentences(List<string> splitText, string word)
        {
            int begin = 0;
            int end = 0;

            // Finds the indicies where the search word is in the article if there are multiple 
            var indicies = splitText.Select((term, index) => new { term, index })
                                    .Where(pair => pair.term == word)
                                    .Select(pair => pair.index);

            var QuoteSentences = new List<string>();

            foreach (int index in indicies)
            {
                for (int pre = index; pre != 0; pre--)
                {
                    if (splitText[pre].Contains('.') || splitText[pre].Contains('!') || splitText[pre].Contains('?'))
                    // Decrements from the found word to find the end of sentence before the context sentence
                    {
                        begin = pre + 1;
                    }
                }

                for (int post = index; post != splitText.Count; post++)
                {
                    if (splitText[post].Contains('.') || splitText[post].Contains('!') || splitText[post].Contains('?'))
                    // Increments from the found word to locate the end of sentence
                    {
                        begin = post;
                    }
                }
                QuoteSentences.Add(string.Join(' ', splitText.GetRange(begin, end - begin)));
            }
            return QuoteSentences.Distinct().ToList();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="textWords"></param>
        /// <param name="searchList"></param>
        /// <returns></returns>
        public static List<string> StringMatchWords(IEnumerable<string> splitText, SearchWordList searchList)
        {
            var thing = searchList.GetAllWordForms()
                        .Where(word => splitText.Contains(word))
                        .OrderBy(t => t)
                        .Where(item => item != null)
                        .ToList();

            Console.WriteLine(thing.Count == 0 ? "No Search Words found" : string.Join('\n', thing));
            return thing;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="textWords"></param>
        /// <param name="searchList"></param>
        /// <returns></returns>
        public static List<string> SoundExMatchWords(IEnumerable<string> splitText, SearchWordList searchList)
        {
            var sx = new Soundex();
            var SoundExWords = splitText.Select(word =>
                { return Tuple.Create(sx.BuildKey(word), word); })
                .OrderBy(tup => tup.Item1);

            Console.WriteLine(string.Join('\n', SoundExWords.Select(tup => tup.Item1 + "\t" + tup.Item2)));

            var SoundExSearch = searchList.GetHeadWords().Select(word =>
                { return Tuple.Create(sx.BuildKey(word), word); })
                .OrderBy(tup => tup.Item1);

            Console.WriteLine(string.Join('\n', SoundExSearch.Select(tup => tup.Item1 + "\t" + tup.Item2)));

            var thing = SoundExWords.Where(word =>
                SoundExSearch.Select(search =>
                search.Item1).Contains(word.Item1))
                .Select(pair => pair.Item2)
                .Where(item => item != null)
                .ToList();

            Console.WriteLine(thing.Count() == 0 ? "No Search Words found using SoundEx Matching"
                : "Search Words found using SoundEx Matching: \n" + string.Join('\n', thing));
            return thing;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="textWords"></param>
        /// <param name="searchList"></param>
        /// <returns></returns>
        public static List<string> MetaphoneMatchWords(IEnumerable<string> splitText, SearchWordList searchList)
        {
            var mp = new DoubleMetaphone();
            var MetaphoneWords = splitText.Select(word =>
                { return Tuple.Create(mp.BuildKey(word), word); })
                .OrderBy(tup => tup.Item1);

            //Console.WriteLine(string.Join('\n', MetaphoneWords.Select(tup => tup.Item1 + "\t" + tup.Item2)));

            var MetaphoneSearch = searchList.GetHeadWords().Select(word =>
                { return Tuple.Create(mp.BuildKey(word), word); })
                .OrderBy(tup => tup.Item1);

            //Console.WriteLine(string.Join('\n', MetaphoneSearch.Select(tup => tup.Item1 + "\t" + tup.Item2)));

            var thing = MetaphoneWords.Where(word =>
                MetaphoneSearch.Select(search =>
                search.Item1).Contains(word.Item1))
                .Select(pair => pair.Item2)
                .Where(item => item != null)
                .ToList();

            Console.WriteLine(thing.Count() == 0 ? "No Search Words found using Metaphone Matching"
               : "Search Words found using Metaphone Matching: \n" + string.Join('\n', thing));
            return thing;
        }

        public static List<string> EditDistMatchWords(IEnumerable<string> splitText, SearchWordList searchList)
        {
            return searchList.GetAllWordForms()
                .Select(word =>
                {
                    foreach (string text in splitText)
                    {
                        if (GetEditDistance(text, word) <= 3)
                        {
                            return word;
                        }
                    }
                    return null;
                }).Where(item => item != null).ToList();
        }

        /// <summary>
        /// Uses the Damerau – Levenshtein Distance Formula to calculate the edit distance between two words
        /// </summary>
        /// <param name="word1"></param>
        /// <param name="word2"></param>
        /// <returns></returns>
        private static int GetEditDistance(string s, string t)
        {
            var bounds = new { Height = s.Length + 1, Width = t.Length + 1 };

            int[,] matrix = new int[bounds.Height, bounds.Width];

            for (int height = 0; height < bounds.Height; height++) { matrix[height, 0] = height; };
            for (int width = 0; width < bounds.Width; width++) { matrix[0, width] = width; };

            for (int height = 1; height < bounds.Height; height++)
            {
                for (int width = 1; width < bounds.Width; width++)
                {
                    int cost = (s[height - 1] == t[width - 1]) ? 0 : 1;
                    int insertion = matrix[height, width - 1] + 1;
                    int deletion = matrix[height - 1, width] + 1;
                    int substitution = matrix[height - 1, width - 1] + cost;

                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));

                    if (height > 1 && width > 1 && s[height - 1] == t[width - 2] && s[height - 2] == t[width - 1])
                    {
                        distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
                    }

                    matrix[height, width] = distance;
                }
            }

            return matrix[bounds.Height - 1, bounds.Width - 1];
        }
    }
}