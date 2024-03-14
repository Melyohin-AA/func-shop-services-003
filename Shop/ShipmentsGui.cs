using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace ShopServices.Shop;

internal static class ShipmentsGui
{
	public static async System.Threading.Tasks.Task<IActionResult> Get()
	{
		var content = ShopServices.IncludedFiles.ResourceAsSring("ShipmentsGui.html");
		return new ContentResult()
		{
			Content = await content,
			ContentType = "text/html; charset=utf-8"
		};
	}
}
