using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ShopServices.Shop.Storing;
using ShopServices.Shop.Storing.Models;

namespace ShopServices.Triggers;

internal static class ShopStoringShipmentModLockApiTrigger
{
	public const string Route = "api/shop/storing/shipment-modlock";

	[FunctionName(nameof(ShopStoringShipmentModLockApiTrigger))]
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
				return Task.FromResult<IActionResult>(
					new BadRequestObjectResult("'deviceId' header/cookie is required"));
			string partition = request.Query["partition"];
			if (string.IsNullOrEmpty(partition))
				return Task.FromResult<IActionResult>(new BadRequestObjectResult("'partition' parameter is required"));
			string shipmentId = request.Query["id"];
			if (string.IsNullOrEmpty(shipmentId))
				return Task.FromResult<IActionResult>(new BadRequestObjectResult("'id' parameter is required"));
			switch (request.Method)
			{
				case "POST":
					return ProcessPost(partition, shipmentId, deviceId, logger);
				case "DELETE":
					return ProcessDelete(partition, shipmentId, deviceId, logger);
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

	private static async Task<IActionResult> ProcessPost(string partition, string shipmentId, string deviceId,
		ILogger logger)
	{
		try
		{
			Shipment shipment = await Storage.AcquireShipmentModLock(partition, shipmentId, deviceId);
			if (shipment == null)
				return new StatusCodeResult(508);
			return new OkObjectResult(shipment.ToJson().ToString(Newtonsoft.Json.Formatting.None));
		}
		catch (Azure.RequestFailedException ex)
		{
			logger.LogError($"Failed to acquire shipment '{partition}/{shipmentId}' modlock: {ex}");
			return new StatusCodeResult(ex.Status);
		}
	}

	private static async Task<IActionResult> ProcessDelete(string partition, string shipmentId, string deviceId,
		ILogger logger)
	{
		try
		{
			await Storage.ReleaseShipmentModLock(partition, shipmentId, deviceId);
			return new NoContentResult();
		}
		catch (Azure.RequestFailedException ex)
		{
			logger.LogError($"Failed to release shipment '{partition}/{shipmentId}' modlock: {ex}");
			return new StatusCodeResult(ex.Status);
		}
	}
}
