/*
imdbfetcher
by mjt, dani [2017]

idea:
eli onnistuuko softa joka etsii kansiosta elokuvat, ja niille hakee IMDBstä arvosanan, juonen ja näyttelijät..
ja tallentaa (esim .txt filuun) ko. tiedot, elokuvan kansioon

IMDB HAKU:   http://www.imdb.com/find?q="leffan nimi"

käytetään omdbapia:
 https://www.codeproject.com/Questions/1105851/How-to-get-movie-information-from-imdb-in-Csharp
   --> http://www.omdbapi.com/?t=Titanic&y=&plot=short&r=json


tiedostonimestä leffan nimi:
  otetaan kirjaimet, jätetään numerot ym roina pois (300 leffaa ei voi sitten hakea)

 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace ImdbFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            ImdbFetcher i = new ImdbFetcher();

            if (args.Length == 0)
                i.Start(Directory.GetCurrentDirectory());
            else
                i.Start(args[0]);
        }
    }

    class ImdbFetcher
    {
        public void Start(string path, bool subDirs = true)
        {
            string[] exts = { "avi", "mpg", "mpeg", "mp4", "mkv", "ts" };
            XmlDocument xmlDoc = new XmlDocument();
            List<string> names = new List<string>();
            SearchOption so = subDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string output;

            string[] filter = System.IO.File.ReadAllLines("filter.txt");
            for (int q = 0; q < filter.Length; q++)
                filter[q] = filter[q].ToUpper();

            foreach (string ext in exts)
            {
                Console.WriteLine("Searching " + ext + "...");
                string[] files = { "" };
                //try
                {
                    files = Directory.GetFiles(path, "*." + ext, so);


                }
                //catch (Exception e) { }

                /*
                 * juuresta ei voi hakea rekursiivisesti:
                        An unhandled exception of type 'System.UnauthorizedAccessException' occurred in mscorlib.dll
                        Additional information: Access to the path 'D:\System Volume Information' is denied.
                 */


                foreach (string s in files)
                {
                    // poista hakemisto:
                    string ns = s.Replace('\\', '/');
                    string newS = ns.Substring(ns.LastIndexOf('/') + 1);

                    // isot kirjaimet
                    newS = newS.ToUpper();

                    // poista sulkeiden sisälmykset
                    output = Regex.Replace(newS, @"(\[[^\]]*\])|(\([^\)]*\))", " ");

                    // poista muuta krääsää
                    foreach (string f in filter)
                    {
                        output = Regex.Replace(output, f, " ");
                    }

                    // poista numerot
                    output = Regex.Replace(output, @"[\d]", " ");

                    output = Regex.Replace(output, @"\.", " "); // pisteet väleiks

                    output = StripWordsWithLessThanXLetters(output, 3); // poista lyhyet sanat

                    names.Add(output);
                    Console.WriteLine(output); //DEBUG
                }
            }
            if (names.Count == 0)
            {
                Console.WriteLine("Movies not found at " + path);
            }
            Console.WriteLine("===============================================================\n\n\n");


            using (System.IO.StreamWriter file = new System.IO.StreamWriter("out.txt"))
            {
                foreach (string movieName in names)
                {
                    //Console.WriteLine("Search " + movieName);

                    if (movieName.Length <= 1) continue;

                    string searchStr = "http://www.omdbapi.com/?t=" + movieName + "&y=&plot=short&r=xml";

                    WebRequest req = HttpWebRequest.Create(searchStr);
                    req.Method = "GET";

                    string source;
                    Console.Write("Search " + movieName);
                    using (StreamReader reader = new StreamReader(req.GetResponse().GetResponseStream()))
                    {
                        source = reader.ReadToEnd();
                        if (source.Contains("root response=\"False\""))
                        {
                            Console.Write("...FAILED.\n");
                            continue;
                        }

                        Console.WriteLine("...save infos.");

                        // parse xml
                        xmlDoc.LoadXml(source);

                        XmlElement root = xmlDoc.DocumentElement;

                        XmlElement pElement;
                        pElement = (XmlElement)root.SelectSingleNode("movie");

                        /*
                        Console.WriteLine("Title: " + pElement.GetAttribute("title") + "     Year: " + pElement.GetAttribute("year"));
                        Console.WriteLine("Imdb Rating: " + pElement.GetAttribute("imdbRating"));
                        Console.WriteLine("Plot: " + pElement.GetAttribute("plot"));
                        Console.WriteLine("Actors: " + pElement.GetAttribute("actors"));
                        //Console.WriteLine(" " + pElement.GetAttribute(""));
                        */

                        file.WriteLine("Title: " + pElement.GetAttribute("title") + "     Year: " + pElement.GetAttribute("year"));
                        file.WriteLine("Imdb Rating: " + pElement.GetAttribute("imdbRating"));
                        file.WriteLine("Plot: " + pElement.GetAttribute("plot"));
                        file.WriteLine("Actors: " + pElement.GetAttribute("actors"));

                    }
                    file.WriteLine("\n======================================================================\n");
                    //Console.WriteLine(source);//DEBUG
                }
                Console.WriteLine("\n\nOK.\nout.txt file saved.\n");

            }
        }

        // http://stackoverflow.com/questions/6344287/c-sharp-regex-remove-words-of-less-than-3-letters
        public static string StripWordsWithLessThanXLetters(string input, int x)
        {
            var inputElements = input.Split(' ');
            var resultBuilder = new StringBuilder();
            foreach (var element in inputElements)
            {
                if (element.Length >= x)
                {
                    resultBuilder.Append(element + " ");
                }
            }
            return resultBuilder.ToString().Trim();
        }
    }
}
