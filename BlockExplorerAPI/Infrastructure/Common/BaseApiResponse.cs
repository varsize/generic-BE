using System.Runtime.Serialization;

namespace BlockExplorerAPI.Infrastructure.Common
{
    [DataContract]
    public class BaseApiResponse
    {
        [DataMember]
        public int code { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public object data { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int status { get; set; }
    }
}