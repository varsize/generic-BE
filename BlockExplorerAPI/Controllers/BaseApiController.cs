using System.Net;
using System.Net.Http;
using System.Web.Http;
using BlockExplorerAPI.Infrastructure;

namespace BlockExplorerAPI.Controllers
{
    public class BaseApiController : ApiController
    {
        protected IHttpActionResult BadRequest(ApiStatusCode code, string message = null)
        {
            var response = Request.CreateResponse(HttpStatusCode.BadRequest, new ApiFailResponse(code, null, message){status = (int)HttpStatusCode.BadRequest});
            return ResponseMessage(response);
        }
    }
}
