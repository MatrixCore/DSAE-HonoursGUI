using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace DSAEHonoursGUI
{
    public class SearchWordList
    {
        // Class that contains a list of SearchWord objects
        // Along with the methods for building them up and searching through the list
        private List<SearchWord> ListSearchWords { get; }

        public SearchWordList(string filePath)
        {
            if (filePath.Contains(".xml"))
            {
                ReadInXMLSearch(filePath, ListSearchWords = new List<SearchWord>());
            } 
            else if (filePath.Contains(".txt"))
            {
                ReadInTextSearch(filePath, ListSearchWords = new List<SearchWord>());
            }
            
        }
        public bool IsEmpty()
        {
            return !ListSearchWords.Any();
        }
        public void AddWord(List<string> tempHead, List<string> tempVars)
        {
            ListSearchWords.Add(new SearchWord(tempHead, tempVars));
        }

        public int CountofWords()
        {
            return GetAllWordForms().Count();
        }

        public override string ToString()
        {
            return string.Join("\n", ListSearchWords);
        }
        /// <summary>
        /// Reads in the XML Search List file and populates the Search Word List object
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        protected static void ReadInXMLSearch(string filePath, List<SearchWord> SearchList)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"{filePath} not found");
                }
                var xDoc = new XmlDocument();
                xDoc.LoadXml(File.ReadAllText(filePath));

                foreach (XmlNode node in xDoc.DocumentElement.SelectNodes("//e"))
                {
                    var headList = new List<string>();
                    foreach (XmlNode headNode in node.SelectNodes("hw[@status='search']")) { headList.Add(headNode.InnerText); }

                    var varList = new List<string>();
                    foreach (XmlNode varNode in node.SelectNodes("v[@status='search']")) { varList.Add(varNode.InnerText); }

                    SearchList.Add(new SearchWord(headList, varList));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("(ReadInXMLFile) " + e.Message);
            }
        }

        protected static void ReadInTextSearch(string filepath, List<SearchWord> SearchList)
        // Reads in a file to build all the different search word objects
        {
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"File name: {filepath} not found");
            }

            List<string> tempHead = new List<string>();
            List<string> tempVars = new List<string>();
            var lines = File.ReadAllLines(filepath);

            foreach (string line in lines)
            // reads the file on a line by line basis
            {
                if (line.Contains("©") || line.Contains("e00001") || line.Contains("AAC")) { }
                // Simple hard skip for the trademark and sample format found in the document

                else if (string.IsNullOrEmpty(line) && tempHead.Count > 0)
                // If it is an empty line, and an ID has been parsed, construct an object
                // and add it to the list
                {
                    SearchList.Add( new SearchWord(tempHead, tempVars));
                    tempVars = new List<string>();
                    tempHead = new List<string>();
                }
                else if (line.Contains("[headword]"))
                // grabs the headword
                {
                    tempHead.Add(line.Substring(0, line.IndexOf('[') - 1).Trim());
                }

                else if ((line.Contains("variant spelling") || line.Contains("plural") || line.Contains("lemma form")) && !line.Contains("[headword]"))
                // Build up the different versions of the headword
                {
                    tempVars.Add(line.Substring(0, line.IndexOf('[')).Trim());
                }
            }
        }

        /// <summary>
        /// Returns each headword and corresponding word varation in a collapsed list 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetAllWordForms()
        {
            return ListSearchWords
                .SelectMany(word => word.Headwords.Concat(word.WordVars));
        }
        /// <summary>
        /// Returns only the heardwords found inside the SearchWordList object
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetHeadWords()
        {
            return ListSearchWords.SelectMany(word => word.Headwords);
        }
        /// <summary>
        /// Method to find the head word of a related word variation
        /// If there are more than one head word, it returns the first in the list
        /// </summary>
        /// <param name="wordVar"></param>
        /// <returns></returns>
        public string HeadWordLookup(string wordVar)
        {
            bool isHeadWord = ListSearchWords.Select(list => list.Headwords.Contains(wordVar)).FirstOrDefault();
            // Checks if the given word is already a head word
            if (isHeadWord) { return wordVar; } // If it is, then return it
            else
            {   // Else it must be a word var, then look for canonical form
                return ListSearchWords
                   .Where(list => list.WordVars.Contains(wordVar))
                   .Select(found => found.Headwords[0])
                   .First();
            }            
        }
        /// <summary>
        /// Subclass to model the DSAE's search list of desired words taken from a text file
        /// </summary>
        public class SearchWord
        // Consider a method of capturing all the tags of WordVars - 'variant spelling generated' or 'plural of headword'
        {
            /// <summary>
            /// Canonical form of the word
            /// </summary>
            public List<string> Headwords { get; private set; }
            /// <summary>
            /// List of the variations of the headword
            /// </summary>
            public List<string> WordVars { get; private set; }
            // TODO: Implement the extraction of Dictionary ID's from the Search List file
            //       Add DictionaryIDs to the XML output
            public string DictionaryID { get; set; }

            /// <summary>
            /// Simple Constructor
            /// </summary>
            /// <param name="HeadWord"></param>
            /// <param name="WordVars"></param>
            public SearchWord(List<string> headWord, List<string> wordVars)
            {
                Headwords = headWord;
                WordVars = wordVars;
            }            

            public override string ToString()
            {
                return (Headwords.Count == 0 ? "" : $"\nHeadword(s): {string.Join(", ", Headwords)}") +
                       (WordVars.Count == 0 ? "" : $"\n\nWord Vars: \n\t{string.Join("\n\t", WordVars)}\n\n\n");
            }
        }
    }
}
