using System.Reflection;
namespace JustAuth.Controllers
{
    public static class Utils
    {
        public static string GetEntryAssemblyPath() {
            return Path.GetDirectoryName( Assembly.GetEntryAssembly().Location);
        }
        
    }
}