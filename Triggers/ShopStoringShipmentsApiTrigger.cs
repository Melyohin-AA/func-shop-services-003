using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ShopServices.Shop.Storing;
using ShopServices.Shop.Storing.Models;

namespace ShopServices.Triggers;

internal static class ShopStoringShipmentsApiTrigger
{
	public const string Route = "api/shop/storing/shipments";

	[FunctionName(nameof(ShopStoringShipmentsApiTrigger))]
	public static async Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = Route)] HttpRequest request,
		ILogger logger)
	{
		try
		{
			if (!Authorization.SimpleAccess.Check(request))
			{
				logger.LogInformation("Failed to auth");
				return new UnauthorizedResult();
			}
			string deviceId = Authorization.DeviceIdentity.GetDeviceId(request);
			if (string.IsNullOrEmpty(deviceId))
				return new BadRequestObjectResult(
					$"'{Authorization.DeviceIdentity.DeviceIdHeader}' header/cookie is required");
			string partition = request.Query["partition"];
			if (string.IsNullOrEmpty(partition))
				return new BadRequestObjectResult("'partition' parameter is required");
			bool getAll = false;
			string shipmentId = null;
			string continuationToken = null;
			string data = null;
			int? group = null;
			bool releaseModLock = false;
			if ((request.Method == "POST") || (request.Method == "PUT"))
			{
				using (var bodyReader = new StreamReader(request.Body, Encoding.UTF8))
					data = await bodyReader.ReadToEndAsync();
				if (request.Method == "PUT")
					releaseModLock = request.Query["release_lock"] == "true";
			}
			else
			{
				if (
					request.Query.TryGetValue("group", out Microsoft.Extensions.Primitives.StringValues values) &&
					(values.Count > 0) && int.TryParse(values, out int parsedResult)
				) group = parsedResult;
				getAll = request.Query["all"] == "true";
				if (getAll)
					continuationToken = request.Query["page"];
				else
				{
					shipmentId = request.Query["id"];
					if (string.IsNullOrEmpty(shipmentId))
						return new BadRequestObjectResult("'id' parameter is required");
				}
			}
			var storage = new Storage(partition, deviceId, logger);

			switch (request.Method)
			{
				case "GET":
					return getAll ?
						await ProcessGetAll(storage, continuationToken, group) :
						await ProcessGet(storage, shipmentId);
				case "POST":
					return await ProcessPost(storage, logger, data);
				case "PUT":
					return await ProcessPut(storage, logger, data, releaseModLock);
				case "DELETE":
					return await ProcessDelete(storage, shipmentId);
			}
			return new ContentResult() {
				StatusCode = 500,
				Content = $"Method '{request.Method}' is not actually supported",
			};
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			return new ContentResult() {
				StatusCode = 500,
				Content = ex.Message,
			};
		}
	}

	private static async Task<IActionResult> ProcessGet(Storage storage, string shipmentId)
	{
		(int code, Shipment shipment) = await storage.GetShipment(shipmentId);
		if (code != 200)
			return new StatusCodeResult(code);
		JObject jsonResult = shipment.ToJson();
		return new OkObjectResult(jsonResult.ToString(Newtonsoft.Json.Formatting.None));
	}

	private static async Task<IActionResult> ProcessGetAll(Storage storage, string continuationToken, int? group)
	{
		(int code, Storage.ShipmentPage shipmentPage) =
			group is int actualGroup ?
			await storage.GetShipmentsInGroup(continuationToken, actualGroup) :
			await storage.GetShipments(continuationToken);
		if (code != 200)
			return new StatusCodeResult(code);
		JObject jsonResult = shipmentPage.ToJson();
		return new OkObjectResult(jsonResult.ToString(Newtonsoft.Json.Formatting.None));
	}

	private static async Task<IActionResult> ProcessPost(Storage storage, ILogger logger, string data)
	{
		if (string.IsNullOrEmpty(data))
			return new BadRequestObjectResult("Body is required");
		Shipment shipment = Shipment.FromJson(JObject.Parse(data));
		shipment.NewId();
		int code = await storage.PostShipment(shipment);
		if (code != 204)
			return new StatusCodeResult(code);
		var jsonResult = new JObject() {
			{ "id", shipment.Id },
			{ "lastModTS", shipment.LastModTS },
		};
		await TryNotifyShipment(logger, shipment, Mailing.NotificationReason.ShipmentCreated);
		return new OkObjectResult(jsonResult.ToString(Newtonsoft.Json.Formatting.None));
	}

	private static async Task<IActionResult> ProcessPut(Storage storage, ILogger logger, string data, bool releaseLock)
	{
		if (string.IsNullOrEmpty(data))
			return new BadRequestObjectResult("Body is required");
		Shipment shipment = Shipment.FromJson(JObject.Parse(data));
		int code = await storage.PutShipment(shipment, releaseLock);
		if (code != 204)
			return new StatusCodeResult(code);
		await TryNotifyShipment(logger, shipment, Mailing.NotificationReason.ShipmentUpdated);
		return new OkObjectResult(shipment.LastModTS);
	}

	private static async Task<IActionResult> ProcessDelete(Storage storage, string shipmentId)
	{
		int code = await storage.DeleteShipment(shipmentId);
		return new StatusCodeResult(code);
	}

	private static async Task<bool> TryNotifyShipment(ILogger logger, Shipment shipment, Mailing.NotificationReason sendReason)
	{
		try
		{
			Mailing.EmailSender email = new(logger);
			Mailing.ShipmentBackupNotifier notifier = new(logger, email);
			await notifier.SendBackupSingleShipmentAsync(sendReason, shipment);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogError($"Unexpected error while sending notification email {ex}");
			return false;
		}
	}
}
