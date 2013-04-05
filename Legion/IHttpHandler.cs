using System;
using System.Collections.Concurrent;
using System.Net;

namespace Legion
{
    public interface IHttpHandler
    {
        string ListenUrl { get; }
        void Handle(HttpListenerRequest request, HttpListenerResponse response);
    }

		public static class IHttpHandlerExtensions
		{
		    private static ConcurrentDictionary<string, string> paths = new ConcurrentDictionary<string, string>();
 
		    public static bool Supports(this IHttpHandler self, string url)
		    {
		        var path = paths.GetOrAdd(self.ListenUrl, key => new Uri(key).AbsolutePath);
		        return url.StartsWith(path);
		    }
		}
}