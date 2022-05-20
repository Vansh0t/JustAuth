using System.Reflection;
namespace JustAuth.Utils
{
    public static class Location
    {
        public static string GetEntryAssemblyPath() {
            return Path.GetDirectoryName( Assembly.GetEntryAssembly().Location);
        }
    }
}