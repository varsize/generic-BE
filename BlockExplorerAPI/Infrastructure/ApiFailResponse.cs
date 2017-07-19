using System.Runtime.Serialization;
using BlockExplorerAPI.Infrastructure.Common;

namespace BlockExplorerAPI.Infrastructure
{
    [DataContract]
    public class ApiFailResponse : BaseApiResponse
    {
        [DataMember]
        public string message { get; private set; }

        public ApiFailResponse(ApiStatusCode code, object data = null, string message = null)
        {
            this.code = (int) code;
            this.message = message ?? code.ToString();
            this.data = data;
        }
    }
}