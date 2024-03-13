using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace ShopServices.Shop;

internal static class ShipmentsGui
{
	public static IActionResult Get()
	{
		return new ContentResult()
		{
			Content = ShopServices.IncludedFiles.ResourceAsSring("ShipmentsGui.html"),
			ContentType = "text/html; charset=utf-8"
		};
	}
}
