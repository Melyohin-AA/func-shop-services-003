using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace ShopServices.Shop;

internal static class ShipmentsGui
{
	public static IActionResult Get()
	{
		var content = ShopServices.IncludedFiles.ResourceAsSring("ShipmentsGui.html");
		content.Wait();
		return new ContentResult()
		{
			Content = content.Result,
			ContentType = "text/html; charset=utf-8"
		};
	}
}
