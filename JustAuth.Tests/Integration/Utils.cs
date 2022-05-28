using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System;

namespace JustAuth.Tests.Integration
{
    public static class Utils
    {
        public static StringContent MakeStringContent(params string[] kvp) {
            if(kvp.Length == 0 || kvp.Length%2!=0) throw new ArgumentException("Invalid kvp length: " + kvp.Length);
            Dictionary<string, string> dict = new();
            for(int i = 0; i < kvp.Length; i+=2) {
                dict.Add(kvp[i], kvp[i+1]);
            }
            var serialized = JsonConvert.SerializeObject(dict);
            return new StringContent(serialized, Encoding.UTF8, "application/json");
        }
    }
}