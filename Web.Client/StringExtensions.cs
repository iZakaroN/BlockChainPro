using System;

namespace BlockChanPro.Web.Client
{
    public static class StringExtensions
    {
	    private const string WebHostPrefix = "http://";

	    public static bool TryParseUrl(this string url, out Uri uri)
	    {
		    if (!url.StartsWith(WebHostPrefix, StringComparison.InvariantCultureIgnoreCase))
			    url = $"{WebHostPrefix}{url}";
		    try
		    {
			    uri = new Uri(url, UriKind.Absolute);
			    return true;
		    } catch(Exception)
			{ /*ignore*/ }

		    uri = null;
			return false;
	    }
	}
}
