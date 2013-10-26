using System;
using System.Collections.Generic;
using System.Linq;
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
            "http://wwjs.badurlwillerror.netf",
            "http://www.reddit.com",
            "http://news.ycombinator.com",
        };

        /// <summary>
        /// Retrieves data from each URL in sequence and returns the entire result set.
        /// </summary>
        public async Task<List<string>> GetAllDataSerial()
        {
            var results = new List<string>();

            foreach (var url in _urls)
            {
                results.Add(await MakeRequest(url));
            }

            return results;
        }

        /// <summary>
        /// Retrieves data from each URL in parallel and returns the entire result set.
        /// </summary>
        public Task<string[]> GetAllDataParallel()
        {
            var results = new List<Task<string>>();

            foreach (var url in _urls)
            {
                results.Add(MakeRequest(url));
            }

            return Task.WhenAll(results);
        }

        /// <summary>
        /// Retrieves data from each URL in sequence and pushs the results to the browser as
        /// they arrive.
        /// </summary>
        public async Task LoadDataOnDemandSerial()
        {
            foreach (var url in _urls)
            {
                SendResponse(await MakeRequest(url));
            }
        }

        /// <summary>
        /// Retrieves data from each URL in parallel and pushes the results to the browser
        /// as they arrive in sequence.
        /// </summary>
        public async Task LoadDataOnDemandParallelRequestsWithSerialCallbacks()
        {
            var tasks = _urls.Select(url => MakeRequest(url))
                             .ToList();

            while (tasks.Count > 0)
            {
                Task<string> completeTask = await Task.WhenAny(tasks);

                tasks.Remove(completeTask);

                SendResponse(await completeTask);
            }
        }

        /// <summary>
        /// Retrieves data from each URL in parallel and pushes the results to the browser
        /// as they arrive in parallel by dispatching each call to a new thread.
        /// </summary>
        public Task LoadDataOnDemandParallelViaDispatch()
        {
            var tasks = new List<Task>(_urls.Length);
            
            foreach (var url in _urls)
            {
                tasks.Add(Task.Run(async () => SendResponse(await MakeRequest(url))));
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Retrieves data from each URL in parallel and pushes the results to the browser
        /// as they arrive in parallel by dispatching each call with a TAP style ContinueWith.
        /// </summary>
        public Task LoadDataOnDemandAllParallelWithParallelCallbacksViaTAP()
        {
            var tasks = new List<Task>(_urls.Length);
            
            foreach (var url in _urls)
            {
                tasks.Add(MakeRequest(url).ContinueWith(t =>
                {
                    if (t.Status != TaskStatus.RanToCompletion)
                    {
                        throw t.Exception;
                    }
                    
                    SendResponse(t.Result);
                }));
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Retrieves data from each URL in parallel and pushes the results to the browser
        /// as they arrive in parallel by an async lambda.
        /// </summary>
        public Task LoadDataOnDemandAllParallelWithParallelCallbacksViaFunc()
        {
            var tasks = new List<Task>(_urls.Length);
            Func<string, Task> go = async url => SendResponse(await MakeRequest(url));

            foreach (var url in _urls)
            {
                tasks.Add(go(url));
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Retrieves data from each URL in parallel and pushes the results to the browser
        /// as they arrive in parallel by LINQ and async lambdas.
        /// </summary>
        public Task LoadDataOnDemandAllParallelWithParallelCallbacksViaLinq()
        {
            return Task.WhenAll(_urls.Select(async url => SendResponse(await MakeRequest(url))));
        }

        /// <summary>
        /// Coming in a future version, you can push results directly to the caller via IProgress`T
        /// </summary>
        public Task LoadDataOnDemandAllParallelViaIProgress(IProgress<string> progress)
        {
            return Task.WhenAll(_urls.Select(async url => progress.Report(await MakeRequest(url))));
        }

        private void SendResponse(string result)
        {
            Clients.Caller.loadResponse(result);
        }

        private async Task<string> MakeRequest(string url)
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    var response = await _client.GetAsync(url, cts.Token);
                    return url + " -> " + response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                return url + " -> " + ex.GetBaseException().Message;
            }
        }
    }
}