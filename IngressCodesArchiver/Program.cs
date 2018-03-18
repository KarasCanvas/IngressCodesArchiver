using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using HtmlAgilityPack;
using System.Linq;

namespace IngressCodesArchiver
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new Program().Run(args);
        }

        private void Run(string[] args)
        {
            Console.Title = "IngressCodesArchiver";
            Console.WriteLine("IngressCodesArchiver started.");
            var postCount = 0;
            var totalHtmlFileSize = 0L;
            var totalPdfFileSize = 0L;
            var config = Config.Instance;
            var currentUrl = config.CurrentUrl;
            var client = new WebClient() { Encoding = Encoding.UTF8 };
            if (String.IsNullOrWhiteSpace(currentUrl))
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        Console.WriteLine("Try to fetch latest post url...");
                        var html = client.DownloadString("https://ingress.codes/");
                        currentUrl = this.ExtractLatestPostUrl(html);
                        Console.WriteLine("Fetch latest post url complated: " + currentUrl);
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0}: {1}", e.GetType().FullName, e.Message);
                    }
                }
            }
            while (!String.IsNullOrWhiteSpace(currentUrl))
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        Console.WriteLine("Download post: " + currentUrl);
                        var html = client.DownloadString(currentUrl);
                        Console.WriteLine("Download completed.");
                        var title = this.ExtractTitle(html);
                        Console.WriteLine("Title: " + title);
                        var prevUrl = this.ExtractPrevPostUrl(html);
                        currentUrl = prevUrl;
                        config.CurrentUrl = prevUrl;
                        config.Save();
                        var htmlFilePath = this.GenerateFilePath(currentUrl, "html", "html");
                        this.CreateDirectoryForFile(htmlFilePath);
                        html = this.CleanPostHtml(html);
                        Console.WriteLine("Save to: " + htmlFilePath);
                        File.WriteAllText(htmlFilePath, html, Encoding.UTF8);
                        var htmlFileInfo = new FileInfo(htmlFilePath);
                        totalHtmlFileSize += htmlFileInfo.Length;
                        Console.WriteLine("File size: " + htmlFileInfo.Length);
                        if (config.ConvertToPdf)
                        {
                            Console.WriteLine("PDF conversion started.");
                            var pdfFilePath = this.GenerateFilePath(currentUrl, "pdf", "pdf");
                            this.CreateDirectoryForFile(pdfFilePath);
                            this.ConvertToPdf(htmlFilePath, pdfFilePath);
                            Console.WriteLine("PDF saved to: " + pdfFilePath);
                            var pdfFileInfo = new FileInfo(pdfFilePath);
                            totalPdfFileSize += pdfFileInfo.Length;
                            Console.WriteLine("File size: " + pdfFileInfo.Length);
                        }
                        postCount++;
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("{0}: {1}", e.GetType().FullName, e.Message);
                    }
                }
                Console.Title = String.Format("IngressCodesArchiver - PostsDownloaded: {0}, TotalHtmlFileSize: {1}, TotalPdfFileSize: {2}", postCount, totalHtmlFileSize, totalPdfFileSize);
            }
            Console.WriteLine("Done.");
            Console.WriteLine("PostsDownloaded: {0}, TotalHtmlFileSize: {1}, TotalPdfFileSize: {2}", postCount, totalHtmlFileSize, totalPdfFileSize);
        }

        private void CreateDirectoryForFile(string filePath)
        {
            var dirPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        private String CleanPostHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            this.RemoveNode(doc, "//header[@id='masthead']");
            this.RemoveNode(doc, "//div[@id='secondary']");
            this.RemoveNode(doc, "//div[@id='footer-area']");
            this.RemoveNode(doc, "//div[contains(@class, 'sharedaddy')]");
            this.RemoveNode(doc, "//div[@id='jp-relatedposts']");
            this.RemoveNode(doc, "//nav[contains(@class, 'post-navigation')]");
            this.RemoveNode(doc, "//img[contains(@class, 'single-featured')]");
            this.RemoveNode(doc, "//script[contains(@src, 'disqus-comment-system/media/js/disqus.js')]");
            this.RemoveNode(doc, "//script[contains(@src, 'disqus-comment-system/media/js/count.js')]");
            this.RemoveNode(doc, "//script[contains(@src, 'jetpack/modules/sharedaddy/sharing.js')]");
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='spoiler-body']");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.SetAttributeValue("style", "");
                }
            }
            nodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'spoiler-head')]");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.RemoveClass("collapsed");
                }
            }
            nodes = doc.DocumentNode.SelectNodes("//script");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.Remove();
                }
            }
            return doc.DocumentNode.OuterHtml;
        }

        private void RemoveNode(HtmlDocument doc, string xpath)
        {
            var node = doc.DocumentNode.SelectSingleNode(xpath);
            if (node != null)
            {
                node.Remove();
            }
        }

        private String ExtractLatestPostUrl(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//main[@id='main']/article[1]/div[1]/a[1]");
            if (node != null)
            {
                return node.GetAttributeValue("href", null);
            }
            return null;
        }

        private String ExtractPrevPostUrl(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//div[@class='nav-previous']/a[1]");
            if (node != null)
            {
                return node.GetAttributeValue("href", null);
            }
            return null;
        }

        private String ExtractTitle(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
            if (node != null)
            {
                return node.GetAttributeValue("content", null);
            }
            return null;
        }

        private String GenerateFilePath(string url, string folder, string extension)
        {
            var uri = new Uri(url);
            var invalidChars = Path.GetInvalidPathChars();
            var chars = (uri.LocalPath ?? String.Empty).Trim(' ', '/', '\\').Replace('/', '_').Where(c => !invalidChars.Contains(c)).ToArray();
            var name = new String(chars);
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder, name + "." + extension);
        }

        private void ConvertToPdf(string htmlFilePath, string pdfFilePath)
        {
            if (File.Exists(pdfFilePath))
            {
                Console.WriteLine("PDF file already existed:", pdfFilePath);
                return;
            }
            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wkhtmltopdf.exe");
            if (!File.Exists(exePath))
            {
                Console.WriteLine("PDF converter 'wkhtmltopdf.exe' is not exists.");
                return;
            }
            var p = new Process();
            p.StartInfo.FileName = exePath;
            p.StartInfo.Arguments = String.Format("{0} {1}", htmlFilePath, pdfFilePath);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            p.WaitForExit();
        }
    }
}
