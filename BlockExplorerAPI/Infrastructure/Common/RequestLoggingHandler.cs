using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using NLog;

namespace BlockExplorerAPI.Infrastructure.Common
{
    public class RequestLoggingHandler : DelegatingHandler
    {
        private static readonly Logger Logger = LogManager.GetLogger("RequestsLog");

        protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var requestMessage = await request.Content.ReadAsStringAsync();

            var sw = new Stopwatch();
            sw.Start();
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            WriteLog(request, requestMessage, sw.ElapsedMilliseconds);
            return response;
        }

        public void WriteLog(HttpRequestMessage request, string requestBody, long milliseconds)
        {
            Logger.Info("{0} {1}. Time: {2} ms. \nHeaders:\n{3}Content:\n{4}",
                request.Method,
                request.RequestUri,
                milliseconds,
                LogHeaders(request.Headers),
                requestBody
            );
        }

        private static string LogHeaders(HttpRequestHeaders headers)
        {
            var stringBuilder = new StringBuilder();
            foreach (var header in headers)
            {
                stringBuilder.AppendFormat("\t{0}: {1}", header.Key, HeaderValueToString(header.Value))
                    .AppendLine();
            }
            return stringBuilder.ToString();
        }

        private static string HeaderValueToString(IEnumerable<string> value)
        {
            var sb = new StringBuilder();
            foreach (var v in value)
            {
                sb.AppendFormat("{0}, ", v);
            }

            if (sb.Length > 1)
                sb.Length = sb.Length - 2;

            return sb.ToString();
        }
    }
}