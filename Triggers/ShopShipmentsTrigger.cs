using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ShopServices.Shop;

namespace ShopServices.Triggers;

internal static class ShopShipmentsTrigger
{
	[FunctionName(nameof(ShopShipmentsTrigger))]
	public static Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shop/shipments")] HttpRequest request,
		ILogger logger)
	{
		return ShipmentsGui.Get();
	}
}
