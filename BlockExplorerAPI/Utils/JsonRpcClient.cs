using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlockExplorerAPI.Utils
{
    public class JsonRpcClient
    {
        private string version;
        private string url;
        private ICredentials credentials;

        public JsonRpcClient(string user, string password, string host, int port, string version = "1.0")
            : this(host, port, version)
        {
            var credentialCache = new CredentialCache();
            credentialCache.Add(new Uri(url), "Basic", new NetworkCredential(user, password));
            credentials = credentialCache;
        }

        public JsonRpcClient(string host, int port, string version)
        {
            url = string.Format("http://{0}:{1}", host, port);
            this.version = version;
            credentials = null;
        }

        public JObject InvokeMethod(string method, params object[] methodParams)
        {
            var job = PrepareMethod(1, method, methodParams);
            return (JObject)SendRequest(job);
        }

        public List<JToken> InvokeMethods(IEnumerable<Tuple<string, object[]>> methods)
        {
            var jar = new JArray();
            int methodId = 1;
            var responseDict = new Dictionary<int, JToken>();
            foreach (var method in methods)
            {
                responseDict.Add(methodId, null);
                var job = PrepareMethod(methodId++, method.Item1, method.Item2);
                jar.Add(job);
            }
            var resultArray = (JArray)SendRequest(jar);
            foreach (var result in resultArray.Children())
            {
                responseDict[result["id"].Value<int>()] = result;
            }            
            return new List<JToken>(responseDict.Values);
        }

        private JObject PrepareMethod(int id, string method, params object[] methodParams)
        {
            var job = new JObject();
            job["jsonrpc"] = version;
            job["id"] = id;
            job["method"] = method;

            if (methodParams != null)
            {
                if (methodParams.Length > 0)
                {
                    var props = new JArray();
                    foreach (var p in methodParams)
                    {
                        props.Add(p);
                    }
                    job.Add(new JProperty("params", props));
                }
            }
            return job;
        }

        private JToken SendRequest(object obj)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Credentials = credentials;
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            string s = JsonConvert.SerializeObject(obj);
            byte[] byteArray = Encoding.UTF8.GetBytes(s);
            webRequest.ContentLength = byteArray.Length;

            using (var dataStream = webRequest.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }

            try
            {
                using (var webResponse = webRequest.GetResponse())
                using (var str = webResponse.GetResponseStream())
                using (var sr = new StreamReader(str))
                {
                    return JToken.Parse(sr.ReadToEnd()); //JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
                }
            }
            catch (WebException webex)
            {
                using (var str = webex.Response.GetResponseStream())
                using (var sr = new StreamReader(str))
                {
                    return JToken.Parse(sr.ReadToEnd());
                }
            }
        }
    }
}
