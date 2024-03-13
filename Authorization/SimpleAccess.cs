using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ShopServices.Authorization;

internal static class SimpleAccess
{
	public const string KeyHeader = "ss-simple-access-key";

	private static bool initialized;
	private static string key;
	public static string Key
	{
		get
		{
			if (!initialized)
			{
				key = Environment.GetEnvironmentVariable("SACCESS_KEY");
				initialized = true;
			}
			return key;
		}
	}

	public static bool Check(string key)
	{
		return (Key == null) || (key == SimpleAccess.key);
	}
	public static bool Check(HttpRequest request)
	{
		return (Key == null) || (GetKeyFromRequest(request) == key);
	}

	public static string GetKeyFromRequest(HttpRequest request)
	{
		if (request.Headers.TryGetValue(KeyHeader, out StringValues hkey))
			return hkey;
		if (request.Cookies.TryGetValue(KeyHeader, out string ckey))
			return ckey;
		return null;
	}
}
