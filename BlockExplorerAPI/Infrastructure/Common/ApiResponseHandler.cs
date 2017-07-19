using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace BlockExplorerAPI.Infrastructure.Common
{
    public class ApiResponseHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            return BuildApiResponse(request, response);
        }

        private static HttpResponseMessage BuildApiResponse(HttpRequestMessage request, HttpResponseMessage response)
        {
            object content;
            response.TryGetContentValue(out content);

            if (content is BaseApiResponse || content is StreamContent)
                return response;

            if (response.IsSuccessStatusCode)
                content = new BaseApiResponse() { data = content };
            else if (response.StatusCode == HttpStatusCode.NotFound)
                content = new BaseApiResponse() { data = content is HttpError ? new { ((HttpError)content).Message } : null, status = (int)response.StatusCode };
            else
                content = new BaseApiResponse() { code = (int)ApiStatusCode.Error, data = content, status = (int)response.StatusCode };

            var newResponse = request.CreateResponse(response.StatusCode, content);
            foreach (var header in response.Headers)
            {
                newResponse.Headers.Add(header.Key, header.Value);
            }
            return newResponse;
        }
    }
}