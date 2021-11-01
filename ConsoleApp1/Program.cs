using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using HtmlAgilityPack;
using OpenQA.Selenium.Support.UI;
using WDSE;
using WDSE.Decorators;
using WDSE.ScreenshotMaker;

namespace ConsoleApp1
{
     public class Pathfinder
     {


          /// A queue of pages to be crawled
          private static Queue<string> queueToCheck = new Queue<string>();


          private static List<string> allCheckedPages = new List<string>();

          // private static Dictionary<string, string> allCheckedPages = new Dictionary<string, string>();

          /// A Url that the crawled page must start with. 
          public static string siteName { get; set; }

          public static List<string> outputList = new List<string>();
          /// Starting page of crawl.
          public static Uri beginning { get; set; }

          /// event that happens when a correct page is visited
          public static Action<string, string> visited = null;

          public static IWebDriver driver = null;
       
          public static void Main()
          {
               int count = 0;

               Pathfinder.visited = (webUrl, content) =>
               {
                    Console.WriteLine("Visited: " + webUrl);
                    string filePath = "File.csv";
                    File.AppendAllText(filePath, webUrl + "\n");


                    // Generates veritcal screen capture and takes screenshot
                    Pathfinder.driver.Manage().Window.Maximize();

                    Pathfinder.driver.Navigate().GoToUrl(webUrl);

                    // Normal screenshot
                  Screenshot screenShot = ((ITakesScreenshot)driver).GetScreenshot();          
                    screenShot.SaveAsFile("C:\\Users\\anquang\\Desktop\\Images\\screenshot" + count++ + ".png", ScreenshotImageFormat.Png);

                    // Fullscreen screenshot
                  //  VerticalCombineDecorator vcd = new VerticalCombineDecorator(new ScreenshotMaker());
                   // Pathfinder.driver.TakeScreenshot(vcd).ToMagickImage().Write("FullscreenScreenshot" + webUrl.Count() + ".png", ImageMagick.MagickFormat.Png);

                    if (webUrl == null)
                    {
                         Pathfinder.driver.Close();
                    }
                    
               };
               Pathfinder.beginning = new Uri("http://www.peanuts.com");
               Pathfinder.siteName = "http://www.peanuts.com";
      
               Pathfinder.driver = new ChromeDriver();

               Pathfinder.driver.Navigate().GoToUrl(Pathfinder.beginning);
               Pathfinder.driver.FindElement(By.CssSelector("#cookie-dismiss")).Click();
               Pathfinder.Start();
               



          }

          public static void Start()
          {
               if (!queueToCheck.Contains(beginning.ToString()))
               {
                    queueToCheck.Enqueue(beginning.ToString());
               }
               var threads = new ThreadStart(PathfinderThread);
               var thread = new Thread(threads);
               thread.Start();


          }

          private static void PathfinderThread()
          {
               while (true)
               {
                    //if there is nothing left in queueToCheck
                    if (queueToCheck.Count == 0)
                    {
                         return;
                    }

                    var webUrl = queueToCheck.First();
                    var http = new WebClient();
                    string html;
                    try
                    {
                         html = http.DownloadString(webUrl);
                    }
                    catch
                    {
                         queueToCheck.Dequeue();
                         allCheckedPages.Add(webUrl);
                         continue;
                    }
                    //remove the url from the queueToCheck of ones to check
                    queueToCheck.Dequeue();
                    if (allCheckedPages.Contains(webUrl))
                    {
                         allCheckedPages.Add(webUrl);
                    }

                  
                    var mimeType = http.ResponseHeaders[HttpResponseHeader.ContentType];
                    if (!mimeType.StartsWith("text/html"))
                    {
                         continue;
                    }

                    //the page passed the tests so it is added
                    visited(webUrl, html);

                    //load the html in the doc
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    var root = webUrl.Substring(0, webUrl.LastIndexOf('/'));

                    //find the links in the page
                    foreach (var link in doc.DocumentNode.SelectNodes("//a[@href]"))
                    {

                         var newLink = link.Attributes["href"].Value;

                         //a whole bunch of things to skip if the newLink satisfies a condition ie. is outside of what we are looking for
                         if (newLink.ToUpper().StartsWith("JAVASCRIPT:"))
                         {
                              continue;
                         }
                         if (newLink.Contains("#"))
                         {
                              continue;
                         }

                         if (newLink.ToUpper().StartsWith("FTP:"))
                         {
                              continue;
                         }
                         if (newLink.ToUpper().StartsWith("MAILTO:"))
                         {
                              continue;
                         }
                         if (newLink.ToUpper().Contains(".PDF"))
                         {
                              continue;
                         }
                         if (!newLink.ToUpper().StartsWith("HTTP://") && !newLink.ToUpper().StartsWith("HTTPS://"))
                         {

                              if (!newLink.StartsWith("/")) newLink = "/" + newLink;
                              newLink = root + newLink;
                         }



                         //if the site goes to something outside of the given starting url
                         if (!newLink.ToUpper().StartsWith(siteName.ToUpper()))
                         {

                              continue;
                         }

                         //skip child pages of pages with query strings
                         if (newLink.Contains("?") && webUrl.Contains("?"))
                         {
                              continue;
                         }

                         //if the page has been visited skip it
                         if (allCheckedPages.Contains(newLink))
                         {
                              
                              continue;
                         }

                         //if the page is already queued up to be crawled skip it
                         if (queueToCheck.Contains(newLink))
                         {
                              // Recent Duplicate
                              continue;
                         }
                         queueToCheck.Enqueue(newLink);
                    }
               }
          }


     }



}