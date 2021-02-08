using DSAEHonoursGUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace DSAE_HonoursGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BeginScrapping();
        }


        private void StopScrap_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BeginScrapping()
        {
            // Begin scrapping 
            try
            {
                var RSSFeeds = new RSS_Class(Directory.GetCurrentDirectory() + @"\files\RSS_Feeds.txt");

                ReportBox.Text += "RSS file loaded succesfully";
                ReportBox.Text += $"\n {RSSFeeds.GetNumRSSfeeds()} RSS feeds taken from RSS file";

                var urlList = RSSFeeds.LoadRSS()
                              .Where(feed => feed != null)
                              .ToList();

                var testurl = new List<Tuple<string, string>>() { urlList.First() };

                ReportBox.Text += $"\n {urlList.Count} URLs loaded from RSS feeds";

                var Searchlist = new SearchWordList(Directory.GetCurrentDirectory() + @"\files\dsae-dictionary-search-list-2020-A-Z.utf8.txt");
                if (!Searchlist.IsEmpty())
                {
                    ReportBox.Text += "\n DSAE Search List file loaded succesfully";
                    ReportBox.Text += $"\n {Searchlist.CountofWords()} head words and word variants found in Search List text file";
                    int numScrapped = 0;
                    var ScrappedList =
                        HTMLScraper.ProcessUrls(testurl).ToList()
                        .Where(item =>
                        {
                            ReportBox.Text += $"\n {++numScrapped} URLs out of {testurl.Count} scrapped succesfully";
                            return item != null;
                        });

                    int numExtracted = 0;
                    var quoteList = ScrappedList
                        .SelectMany(data =>
                        {
                            ReportBox.Text += $"\n {++numExtracted} articles out of {urlList.Count} quotes extracted";
                            return HTMLScraper.FindSearchWords(data, Searchlist, 0);
                        })
                            .ToList();

                    Quote.OutputXML(Directory.GetCurrentDirectory() + $@"\files\Output\test.xml", quoteList);

                    ReportBox.Text += "\n Extracted quotes successfully written to \\files\\Output\\";
                    ReportBox.Text += $"\n Total number of generated quotes: {quoteList.Count()}";
                };

                ReportBox.Text += "\n Scrapping complete";
            }
            catch (Exception exp)
            {
                ReportBox.Text += $"\n Scrapping has failed with this message:{exp}";
            }
        }
    }
}
