using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.Services.Client;
using System.Configuration;
using ContextAwareSearchWebAPI.Models;

namespace ContextAwareSearchWebAPI.Controllers
{
    public class SearchResultController : ApiController
    {
        [HttpGet]
        public SearchResult[] ContextAwareSearch([FromUri] string query, [FromUri] string[] keywords)
        {
            query = query.Trim();
            List<string> queryModifications = new List<string>();
            queryModifications.Add("learning " + query);
            queryModifications.Add(query + " events");
            queryModifications.Add(query + " basics");
            queryModifications.Add(query + " research");
            queryModifications.Add(query + " courses");

            string[] context = {"coursera", "instructables", "edx", "udacity", "itunesu", "github", "khan academy"};

            //Set default web proxy - ONLY NEEDED FOR 1AND1 HOSTING
            //WebRequest.DefaultWebProxy = new WebProxy("ntproxyus.lxa.perfora.net", 3128);

            //Set up Bing connection
            string rootUri = "https://api.datamarket.azure.com/Bing/Search";
            var bingContainer = new Bing.BingSearchContainer(new Uri(rootUri));
            var accountKey = ConfigurationManager.AppSettings["BING_KEY"];
            bingContainer.Credentials = new NetworkCredential(accountKey, accountKey);

            //Set up search results list
            List<IEnumerable<Bing.WebResult>> searchResults = new List<IEnumerable<Bing.WebResult>>();

            //Search for given topic
            DataServiceQuery<Bing.WebResult> webQuery = bingContainer.Web(query, null, null, "en-us", null, null, null, null);
            webQuery = webQuery.AddQueryOption("$top", 20);
            searchResults.Add(webQuery.Execute());

            //Search for keywords
            foreach (string keyword in keywords)
            {
                webQuery = bingContainer.Web(query + keyword.Trim(), null, null, "en-us", null, null, null, null);
                webQuery = webQuery.AddQueryOption("$top", 20);
                searchResults.Add(webQuery.Execute());
            }

            //Add using query modifications
            foreach (string queryMod in queryModifications)
            {
                webQuery = bingContainer.Web(queryMod, null, null, "en-us", null, null, null, null);
                webQuery = webQuery.AddQueryOption("$top", 20);
                searchResults.Add(webQuery.Execute());
            }

            //Parse search results
            List<SearchResult> items = new List<SearchResult>();
            int listNumber = 1;
            for (int i = 0; i < searchResults.Count; i++)
            {
                int initialRank = listNumber;
                foreach (Bing.WebResult result in searchResults[i])
                {
                    int rank = initialRank;
                    SearchResult temp = new SearchResult();
                    temp.title = result.Title;
                    temp.description = result.Description;
                    temp.url = result.Url;
                    
                    //Modify rank based on user preferences
                    foreach (string keyword in keywords)
                    {
                        if (result.Title.ToLower().Contains(keyword))
                        {
                            rank = rank / 4;
                        }
                        else if (result.Description.ToLower().Contains(keyword))
                        {
                            rank = rank / 2;
                        }
                    }

                    //Modify rank based on static context
                    foreach (string word in context)
                    {
                        if (result.Url.ToLower().Contains(word))
                        {
                            rank = rank / 10;
                        }
                        else if (result.Title.ToLower().Contains(word))
                        {
                            rank = rank / 4;
                        }
                        else if (result.Description.ToLower().Contains(word))
                        {
                            rank = rank / 2;
                        }
                    }

                    if (result.Url.ToLower().Contains("youtube"))
                    {
                        rank = rank * 100;
                    }
                    else if (result.Title.ToLower().Contains("youtube"))
                    {
                        rank = rank * 100;
                    }
                    else if (result.Description.ToLower().Contains("youtube"))
                    {
                        rank = rank * 100;
                    }

                    temp.ranking = rank;
                    items.Add(temp);
                    initialRank += 100;
                }
                listNumber++;
            }

            //Sort results by rank
            items.Sort((s1, s2) => s1.ranking.CompareTo(s2.ranking));
            List<string> results = new List<string>();
            foreach (SearchResult item in items)
            {
                results.Add(item.title + "\n" + item.description + "\n" + item.url + "\n\n");
            }

            SearchResult[] resultArray = items.ToArray();
            return resultArray;
        }
    }
}
