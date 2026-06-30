using System;
using System.IO;
using System.Net.Http;
using System.Text;
using FlowNode.node.Attribute;

namespace FlowNode.node
{
    /// <summary>
    /// HTTP 与路径工具（自动运行的纯数据节点）。
    /// </summary>
    [Node("Io")]
    public class HttpOperator
    {
        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FlowNode/1.0");
            return client;
        }

        [Function("httpGet", true)]
        public static void HttpGet(string url, out string result)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                result = string.Empty;
                return;
            }

            try
            {
                result = Client.GetStringAsync(url).GetAwaiter().GetResult() ?? string.Empty;
            }
            catch
            {
                result = string.Empty;
            }
        }

        [Function("httpPost", true)]
        public static void HttpPost(string url, string body, out string result)
        {
            result = string.Empty;
            if (string.IsNullOrWhiteSpace(url))
                return;

            try
            {
                var content = new StringContent(body ?? string.Empty, Encoding.UTF8, "application/json");
                var response = Client.PostAsync(url, content).GetAwaiter().GetResult();
                result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            }
            catch
            {
                result = string.Empty;
            }
        }
    }
}
