using Microsoft.AspNetCore.Mvc;

namespace ShopServices.Shop;

internal static class CustomerOrdersGui
{
	public static IActionResult Get()
	{
		var content = ShopServices.IncludedFiles.ResourceAsSring("CustomerOrdersGui.html");
		content.Wait();
		return new ContentResult()
		{
			Content = content.Result,
			ContentType = "text/html; charset=utf-8"
		};
	}
}
