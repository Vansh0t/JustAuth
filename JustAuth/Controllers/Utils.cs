using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace JustAuth.Controllers
{
    public static class Utils
    {
        public static string GetEntryAssemblyPath() {
            return Path.GetDirectoryName( Assembly.GetEntryAssembly().Location);
        }
    }
}