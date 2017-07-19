using System;
using Newtonsoft.Json.Linq;

namespace BlockExplorerAPI.Utils
{
    public static class JsonHelpers
    {
        public static bool IsEmpty(this JToken token)
        {
            return (token == null) ||
                   (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues) ||
                   (token.Type == JTokenType.String && token.ToString() == String.Empty) ||
                   (token.Type == JTokenType.Null);
        }
    }
}