using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ShopServices.Authorization;

internal static class DeviceIdentity
{
	public const string DeviceIdHeader = "ss-device-id";

	public static string GetDeviceId(HttpRequest request)
	{
		if (request.Headers.TryGetValue(DeviceIdHeader, out StringValues hkey))
			return hkey;
		if (request.Cookies.TryGetValue(DeviceIdHeader, out string ckey))
			return ckey;
		return null;
	}
}
