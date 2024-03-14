using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ShopServices.Shop;

namespace ShopServices.Triggers;

internal static class ShopCustomerOrdersTrigger
{
	[FunctionName(nameof(ShopCustomerOrdersTrigger))]
	public static Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shop/customer-orders")] HttpRequest request,
		ILogger logger)
	{
		return CustomerOrdersGui.Get();
	}
}
