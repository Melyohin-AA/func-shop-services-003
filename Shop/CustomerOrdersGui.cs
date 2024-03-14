using Microsoft.AspNetCore.Mvc;

namespace ShopServices.Shop;

internal static class CustomerOrdersGui
{
	public static IActionResult Get()
	{
		return new ContentResult()
		{
			Content = ShopServices.IncludedFiles.ResourceAsSring("CustomerOrdersGui.html"),
			ContentType = "text/html; charset=utf-8"
		};
	}
}
