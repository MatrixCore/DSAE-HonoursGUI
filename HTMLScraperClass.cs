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
namespace DSAEHonoursGUI
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
        public static IEnumerable<ScrappedData> ProcessUrls(IEnumerable<Tuple<string, string>> URLs)
        {
            // Poential change: Don't save the whole document, rather use getTextNodes() to grab the article body and drop the request of the HTML document as nothing else is being used 
            // Still archive the HTML document but only keep the article body in memory
            return URLs.Select(address =>
            {
                try
                {
                    bool doProcess = true;
                    bool testLogURL = false;
                    var web = new HtmlWeb();
                    var html = web.Load(address.Item1);
                    if (web.StatusCode != HttpStatusCode.OK) { throw new Exception($"Web status (processUrls): {web.StatusCode}"); }
                    // Check for 302 or 500 code
                    // Gets last HTTP status code and throws exception if not == OK (200)
                    var text = GetTextNode(html, address.Item2.Trim());

                    if (testLogURL)
                    {
                        if (!File.Exists(Directory.GetCurrentDirectory() + @"\files\URLtable.txt"))
                        {
                            File.Create(Directory.GetCurrentDirectory() + @"\files\URLtable.txt");
                        }
                        else
                        {
                            doProcess = !File.ReadAllLines(Directory.GetCurrentDirectory() + @"\files\URLtable.txt")
                            .ToList().Contains(address.Item1);
                            // Used to control further processing, if the url is already recorded then do not process further 
                        }

                        if (doProcess)
                        {
                            TextWriter writer = new StreamWriter(Directory.GetCurrentDirectory() + @"\files\URLtable.csv", true);
                            writer.WriteLine($"{address.Item1}, {DateTime.Today.ToString("d")}");
                            writer.Close();
                            return new { Doc = html, Url = address, body = text };
                        }
                        else
                        {
                            return null;
                        }
                    }
                    return new { Doc = html, Url = address, body = text };
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"(processUrls) {address}: {e.Message}");
                    return null;
                }
            })
            .Select(data =>
             { // Search through the head tag to find meta data
                 if (data == null) { return null; }
                 return new ScrappedData(
                     // Publication date tags vary from outlet to outlet so once the common data 
                     // has been extracted, the html page is passed to a method to record the published 
                     title: data?.Doc?.DocumentNode?.SelectSingleNode("//head/title")?.InnerText,
                     author: data?.Doc?.DocumentNode?.SelectSingleNode("//head/meta[@name='author']")?.Attributes["content"]?.Value,
                     description: data?.Doc?.DocumentNode?.SelectSingleNode("//head/meta[@name='description']")?.Attributes["content"]?.Value,
                     date: GetPublishedDate(data?.Doc, data?.Url.Item2.Trim()),
                     text: data?.body,
                     url: data?.Url.Item1,
                     rss: data?.Url.Item2
                     );
             });
        }

        /// <summary>
        /// Due to the different ways web pages store their publication date, this method needs to pull that info depending on which publciation the HTML comes from
        /// </summary>
        /// <param name="incomplete"></param>
        /// <param name="doc"></param>
        /// <returns>Returns a the string of the publication date</returns>
        public static string GetPublishedDate(HtmlDocument Doc, string source)
        {
            try
            {
                if (source.Contains("News24"))
                {
                    return ScrappedData.formatPublishedDate(
                        Doc?.DocumentNode?.SelectSingleNode("//head/meta[@name='publisheddate']")?.Attributes["content"]?.Value, source);
                }
                else if (source.Contains("Eyewitness News") || source.Contains("IOL"))
                {
                    return ScrappedData.formatPublishedDate(
                        Doc?.DocumentNode?.SelectSingleNode("//meta[@itemprop = 'datePublished']")?.Attributes["content"]?.Value, source);
                }
                else if (source.Contains("BusinessLIVE") || source.Contains("TimesLIVE"))
                {
                    return ScrappedData.formatPublishedDate(
                            Doc.DocumentNode.SelectSingleNode("//div[@class = 'article-pub-date ']").Attributes["content"].Value, source); // Not working for either
                }
                else if (source.Contains("SowetanLIVE"))
                {
                    return ScrappedData.formatPublishedDate(
                            Doc.DocumentNode.SelectSingleNode("//span[@class = 'article-pub-date']").Attributes["content"].Value, source); // Not working
                }
                else
                {
                    return "Unknown";
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Article publish date not found");
                return "Unknown";
            }
        }

        /// <summary>
        /// Returns all the text found in the article body of a single News Story
        /// </summary>
        /// <param name="DocNode"></param>
        /// <returns>The inner next of what would the article node of the respective article</returns>
        public static string GetTextNode(HtmlDocument doc, string RSSsource)
        {
            // EWN -> <span itemprop="articleBody">                             
            // News24 -> <div class="article__body">                            
            // BusinessLive -> <div class=wrap>  <div class=text>               
            // IOL -> <div itemProp="articleBody">                              
            // TimesLive -> Same as BusinessLive and SowetanLive
            //           -> <div[class = 'wrap']> <div[class = 'text']>
            try
            {
                if (RSSsource.Contains("Eyewitness"))
                {
                    return doc.DocumentNode.SelectSingleNode("//span[@itemprop = 'articleBody']").InnerText;
                }
                else if (RSSsource.Contains("News24"))
                {
                    // Regular article 
                    if (doc.DocumentNode.SelectSingleNode("//div[@class = 'article__body']").InnerText != null)
                    // Checks to see if the above code returns a string
                    {
                        return doc.DocumentNode.SelectSingleNode("//div[@class = 'article__body']").InnerText;
                    }
                    else // Otherwise the article is paywalled 
                    {
                        return doc.DocumentNode.SelectSingleNode("//div[@class = 'article__body--locked']").InnerText;
                        // If the article is locked behind a paywall, then a different div attritubte is used
                    }
                }
                else if (RSSsource.Contains("BusinessLIVE"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@class = 'wrap']/div[@class = 'text']").InnerText;
                }
                else if (RSSsource.Contains("TimesLIVE"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@class = 'wrap']/div[@class = 'text']").InnerText;
                }
                else if (RSSsource.Contains("SowetanLIVE"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@class = 'wrap']/div[@class = 'text']").InnerText;
                }
                else if (RSSsource.Contains("IOL"))
                {
                    return doc.DocumentNode.SelectSingleNode("//div[@itemprop = 'articleBody']").InnerText;
                }

                else { throw new Exception("Unrecognized RSS feed source"); }
            }
            catch (Exception e)
            {
                Console.WriteLine("(GetTextNodes) " + e.Message);
                return null;
            }
        }
        /// <summary>
        /// Method used to remove unwanted HTML artifcats and adds in whitespaces where needed for further processing
        /// </summary>
        /// <param name="Splittext"></param>
        /// <returns></returns>
        private static string SanitizeText(string Splittext)
        {
            //cases like letter.letter or letter." or letter.'
            // Use Regex as you have more control
            // Something like: {letter}?{letter} is replaced with {letter}?{whitespace}{letter}
            // OR HTML Ampersand Character Codes like &quot
            // also escaped double quote marks
            string originalText = Splittext;
            try
            {
                Splittext = Splittext.Replace("&nbsp", " ");
                Splittext = Splittext.Replace("&ndash", "-");

                var periodRegex = new Regex("(Letter1)[a-zA-Z].(Letter2)[a-zA-Z]");
                var questionRegex = new Regex("(Letter1)[a-zA-Z]?(Letter2)[a-zA-Z]");
                var exclamationRegex = new Regex("(Letter1)[a-zA-Z]!(Letter2)[a-zA-Z]");

                //Splittext = periodRegex.Replace(Splittext, $"{Letter1}.{letter2}");
                //Splittext = questionRegex.Replace(Splittext, $"{Letter1}.{letter2}");
                //Splittext = exclamationRegex.Replace(Splittext, $"{Letter1}.{letter2}");

                return Splittext.Replace(",", " ");
            }
            catch (Exception e)
            {
                Console.WriteLine("(SanitizeText)" + e.Message);
                return originalText;
                // if the santizion fails, just return the original string and continue
            }
        }

        /// <summary>
        /// Method used to seperate article text and examine each word for a match in the search list using different methods
        /// </summary>
        public static List<Quote> FindSearchWords(ScrappedData data, SearchWordList searchWordList, int mode)
        {
            var splitText = //SanitizeText(data.ArticleText)
                      data.ArticleText
                     .Split(' ')
                     .Where(str => !string.IsNullOrWhiteSpace(str))
                     .Where(str => str.Length > 1) // filter out single chars
                     .ToList();
            try
            {
                // TODO: Add logic to handle search words that a multiple words in length
                // Perhaps search by first word and then check if following word matches
                switch (mode)
                {
                    case 0:
                        return GenerateQuotes(StringMatchWords(splitText, searchWordList), data, splitText, searchWordList);

                    case 1:
                        return GenerateQuotes(SoundExMatchWords(splitText, searchWordList), data, splitText, searchWordList);

                    case 2:
                        return GenerateQuotes(MetaphoneMatchWords(splitText, searchWordList), data, splitText, searchWordList);

                    case 3:
                        return GenerateQuotes(EditDistMatchWords(splitText, searchWordList), data, splitText, searchWordList);

                    default:
                        throw new Exception("Invalid matching mode value. Please select {1, 2, 3, 4}");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("(GetFoundWords) " + e.Message);
                return null;
            }
        }
        /// <summary>
        /// Packages the article metadata, found words and the context sentence into a Quote object 
        /// </summary>
        /// <param name="foundWords"></param>
        /// <param name="data"></param>
        /// <returns>Returns a list of every found word and the corresponding object</returns>
        public static List<Quote> GenerateQuotes(List<string> foundWords, ScrappedData data, List<string> splitText, SearchWordList SearchList)
        {
            try
            {
                return foundWords.Distinct().Select(word =>
                                          new Quote(word,
                                          FindQuoteSentences(splitText, word),
                                          data.PublishedDate,
                                          data.Author,
                                          data.Title,
                                          data.RSS_Source,
                                          data.URL))
                                          .ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine("(GenerateQuotes) " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Method to return multiple sentences the search word was found in by looking forwards and backwards for punctuation
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
                        break;
                    }
                }

                for (int post = index; post != splitText.Count; post++)
                {
                    if (splitText[post].Contains('.') || splitText[post].Contains('!') || splitText[post].Contains('?'))
                    // Increments from the found word to locate the end of sentence
                    {
                        end = post;
                        break;
                    }
                }
                QuoteSentences.Add(string.Join(" ", splitText.GetRange(begin, end - begin)));
            }
            return QuoteSentences.Distinct().ToList();
        }
        /// <summary>
        /// Uses simple string matching to find words using the search list
        /// </summary>
        /// <param name="textWords"></param>
        /// <param name="searchList"></param>
        /// <returns></returns>
        public static List<string> StringMatchWords(IEnumerable<string> splitText, SearchWordList searchList)
        {
            //return searchList.GetAllWordForms()
            //       .Where(word => splitText.Contains(word))
            //       .ToList();
            var final = new List<string>();
            foreach (string form in searchList.GetAllWordForms())
            {
                foreach (string word in splitText)
                {
                    if (form == word)
                    {
                        final.Add(word);
                    }
                }
            }
            return final;

            //Console.WriteLine(thing.Count == 0 ? "No Search Words found" : string.Join("\n", thing));
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

            Console.WriteLine(string.Join("\n", SoundExWords.Select(tup => tup.Item1 + "\t" + tup.Item2)));

            var SoundExSearch = searchList.GetHeadWords().Select(word =>
                { return Tuple.Create(sx.BuildKey(word), word); })
                .OrderBy(tup => tup.Item1);

            Console.WriteLine(string.Join("\n", SoundExSearch.Select(tup => tup.Item1 + "\t" + tup.Item2)));

            var thing = SoundExWords.Where(word =>
                SoundExSearch.Select(search =>
                search.Item1).Contains(word.Item1))
                .Select(pair => pair.Item2)
                .Where(item => item != null)
                .ToList();

            Console.WriteLine(thing.Count() == 0 ? "No Search Words found using SoundEx Matching"
                : "Search Words found using SoundEx Matching: \n" + string.Join("\n", thing));
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
               : "Search Words found using Metaphone Matching: \n" + string.Join("\n", thing));
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