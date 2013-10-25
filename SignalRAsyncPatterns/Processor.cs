using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace SignalRAsyncPatterns
{
    public class Processor : Hub
    {
        private HttpClient _client = new HttpClient();

        private static string[] _urls = new string[]
        {
            "http://www.microsoft.com",
            "http://www.google.com",
            "http://www.bing.com",
            "http://www.reddit.com",
            "http://news.ycombinator.com",
        };

        public async Task<List<string>> GetAllData()
        {
            var results = new List<string>();
            foreach (var url in _urls)
            {
                results.Add(await MakeRequest(url));
            }
            return results;
        }

        public Task<string[]> GetAllDataParallel()
        {
            var results = new List<Task<string>>();
            foreach (var url in _urls)
            {
                results.Add(MakeRequest(url));
            }

            return Task.WhenAll(results);
        }

        public async Task LoadDataOnDemand()
        {
            foreach (var url in _urls)
            {
                Clients.Caller.loadResponse(await MakeRequest(url));
            }
        }

        public Task LoadDataOnDemandParallel()
        {
            var tasks = new List<Task>(_urls.Length);
            
            foreach (var url in _urls)
            {
                tasks.Add(Task.Run(async () =>
                {
                    Clients.Caller.loadResponse(await MakeRequest(url));
                }));
            }

            return Task.WhenAll(tasks);
        }

        private async Task<string> MakeRequest(string url)
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                {
                    var response = await _client.GetAsync(url, cts.Token);
                    return url + " -> " + response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                return url + " -> " + ex.ToString();
            }
        }
    }
}