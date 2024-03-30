using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ShopServices.Shop.Storing;

namespace ShopServices.Triggers;

internal static class ShopStoringShipmentLockApiTrigger
{
	public const string Route = "api/shop/storing/shipment-lock";

	[FunctionName(nameof(ShopStoringShipmentLockApiTrigger))]
	public static Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", "delete", Route = Route)] HttpRequest request,
		ILogger logger)
	{
		try
		{
			if (!Authorization.SimpleAccess.Check(request))
			{
				logger.LogInformation("Failed to auth");
				return Task.FromResult<IActionResult>(new UnauthorizedResult());
			}
			string deviceId = Authorization.DeviceIdentity.GetDeviceId(request);
			if (string.IsNullOrEmpty(deviceId))
				return Task.FromResult<IActionResult>(new BadRequestObjectResult(
					$"'{Authorization.DeviceIdentity.DeviceIdHeader}' header/cookie is required"));
			string partition = request.Query["partition"];
			if (string.IsNullOrEmpty(partition))
				return Task.FromResult<IActionResult>(new BadRequestObjectResult("'partition' parameter is required"));
			string shipmentId = request.Query["id"];
			if (string.IsNullOrEmpty(shipmentId))
				return Task.FromResult<IActionResult>(new BadRequestObjectResult("'id' parameter is required"));
			var storage = new Storage(partition, deviceId, logger);
			switch (request.Method)
			{
				case "POST":
					return ProcessPost(storage, shipmentId);
				case "DELETE":
					return ProcessDelete(storage, shipmentId);
			}
			return Task.FromResult<IActionResult>(new ContentResult() {
				StatusCode = 500,
				Content = $"Method '{request.Method}' is not actually supported",
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			return Task.FromResult<IActionResult>(new ContentResult() {
				StatusCode = 500,
				Content = ex.Message,
			});
		}
	}

	private static async Task<IActionResult> ProcessPost(Storage storage, string shipmentId)
	{
		int code = await storage.AcquireShipmentLock(shipmentId);
		return new StatusCodeResult(code);
	}

	private static async Task<IActionResult> ProcessDelete(Storage storage, string shipmentId)
	{
		int code = await storage.ReleaseShipmentLock(shipmentId);
		return new StatusCodeResult(code);
	}
}
