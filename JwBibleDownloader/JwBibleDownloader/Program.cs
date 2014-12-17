namespace JwBibleDownloader
{
    using HtmlAgilityPack;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    internal static class Program
    {
        // For debugging use - that can used to download a particular chapter for verification
        //private static int debugGroupId = 0;
        //private static int debugBookId = 0;
        //private static int debugChapterId = 0;

        private static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        private static async Task MainAsync()
        {
            // TODO: Replace it with your language
            string bookLink = "http://www.jw.org/en/publications/bible/nwt/books/";
            // string bookLink = "http://www.jw.org/zh-hans/%E5%87%BA%E7%89%88%E7%89%A9/%E5%9C%A3%E7%BB%8F/nwt/%E5%9C%A3%E7%BB%8F%E7%BB%8F%E5%8D%B7/";
            Bible bible = new Bible();
            bible.Groups = new List<Group>();
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(1);
                var result = await client.GetAsync(bookLink);
                HtmlDocument indexPageDocument = new HtmlDocument();
                indexPageDocument.LoadHtml(await result.Content.ReadAsStringAsync());
                foreach (var group in new string[] { "hebScr", "grkScr" })
                {
                    //if (++debugGroupId != 1)
                    //{
                    //    continue;
                    //}

                    Group currentGroup = new Group();
                    currentGroup.Books = new List<Book>();
                    bible.Groups.Add(currentGroup);
                    var sprites = indexPageDocument.DocumentNode.SelectNodes("//div[@class='booksGrouping " + group + "']//span[@class='accordionSprite']");
                    foreach (var sprite in sprites)
                    {
                        //if ((++debugBookId) != 18)
                        //{
                        //    continue;
                        //}

                        Book currentBook = new Book();
                        currentBook.Chapters = new List<Chapter>();
                        currentGroup.Books.Add(currentBook);
                        var bookName = sprite.PreviousSibling.InnerText;
                        currentBook.Name = bookName;
                        Console.WriteLine(bookName);
                        foreach (var chaptersNode in sprite.ParentNode.ParentNode.NextSibling.NextSibling.ChildNodes.First(n => n.Name == "ul").ChildNodes.Where(lc => lc.Name == "li"))
                        {
                            //if ((++debugChapterId) != 34)
                            //{
                            //    continue;
                            //}

                            string chaptersName = chaptersNode.ChildNodes[0].InnerText;
                            Console.WriteLine(chaptersName);
                            Chapter currentChapter = new Chapter();
                            currentChapter.Verses = new List<string>();
                            currentBook.Chapters.Add(currentChapter);
                            Uri fullLink = new Uri(new Uri("http://www.jw.org/"), chaptersNode.ChildNodes[0].Attributes["href"].Value);
                            bool contentNotAvailable = true;
                            while (contentNotAvailable)
                            {
                                try
                                {
                                    var bookPage = await client.GetAsync(fullLink.ToString());
                                    contentNotAvailable = false;
                                    HtmlDocument bookChapterDocument = new HtmlDocument();
                                    bookChapterDocument.LoadHtml(await bookPage.Content.ReadAsStringAsync());
                                    var bibleTextNode = bookChapterDocument.DocumentNode.SelectNodes("//div[@id='bibleText']")[0];
                                    int verseNumber = 0;
                                    foreach (var verse in bibleTextNode.ChildNodes.Where(bc => bc.Name.Equals("span")))
                                    {
                                        verseNumber++;
                                        StringBuilder verseBuilder = new StringBuilder();
                                        verseBuilder.Append(verseNumber);
                                        foreach (var verseNode in verse.ChildNodes)
                                        {
                                            foreach (var node in verseNode.ChildNodes)
                                            {
                                                if (node.Name == "sup" && node.Attributes["class"] != null && node.Attributes["class"].Value == "verseNum")
                                                {
                                                    continue;
                                                }

                                                if (node.Name == "span" && node.Attributes["class"] != null && node.Attributes["class"].Value == "chapterNum")
                                                {
                                                    continue;
                                                }

                                                if (node.Name == "a" && node.Attributes["class"] != null && node.Attributes["class"].Value == "xrefLink jsBibleLink")
                                                {
                                                    continue;
                                                }

                                                verseBuilder.Append(node.InnerText);
                                            }
                                        }
                                        currentChapter.Verses.Add(verseBuilder.ToString());
                                    };
                                }
                                catch
                                {
                                    // ... just retry
                                }
                            }
                        }
                    }
                }
            }
            using (StringWriter sw = new StringWriter())
            {
                new JsonSerializer().Serialize(sw, bible);

                // TODO: Find a right place to store the downloaded result
                File.WriteAllText(@"c:\temp\bible.json", sw.ToString());
            }
        }
    }

    class Bible
    {
        public List<Group> Groups { get; set; }
    }

    class Group
    {
        public List<Book> Books { get; set; }
    }

    class Book
    {
        public List<Chapter> Chapters { get; set; }

        public string Name { get; set; }
    }

    class Chapter
    {
        public List<string> Verses { get; set; }
    }
}

