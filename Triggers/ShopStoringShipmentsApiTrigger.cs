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
	public static Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = Route)] HttpRequest request,
		ILogger logger)
	{
		try
		{
			if (!Authorization.SimpleAccess.Check(request))
			{
				logger.LogInformation("Failed to auth");
				return Task.FromResult<IActionResult>(new UnauthorizedResult());
			}
			//Azure.Core.TokenCredential cred = new Azure.Identity.DefaultAzureCredential();
			//logger.LogInformation($"[{cred.GetToken}]");
			string partition = request.Query["partition"];
			if (string.IsNullOrEmpty(partition))
				return Task.FromResult<IActionResult>(new BadRequestObjectResult("'partition' parameter is required"));
			bool getAll;
			string shipmentId;
			string continuationToken;
			string data;
			if ((request.Method == "POST") || (request.Method == "PUT"))
			{
				getAll = false;
				shipmentId = continuationToken = null;
				using (var bodyReader = new StreamReader(request.Body, Encoding.UTF8))
					data = bodyReader.ReadToEnd();
			}
			else
			{
				getAll = request.Query["all"] == "true";
				if (getAll)
				{
					shipmentId = null;
					continuationToken = request.Query["page"];
				}
				else
				{
					shipmentId = request.Query["id"];
					if (string.IsNullOrEmpty(shipmentId))
						return Task.FromResult<IActionResult>(new BadRequestObjectResult("'id' parameter is required"));
					continuationToken = null;
				}
				data = null;
			}
			switch (request.Method)
			{
				case "GET":
					return getAll ?
						ProcessGetAll(partition, continuationToken, logger) :
						ProcessGet(partition, shipmentId, logger);
				case "POST":
					return ProcessPost(partition, data, logger);
				case "PUT":
					return ProcessPut(partition, data, logger);
				case "DELETE":
					return ProcessDelete(partition, shipmentId, logger);
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

	private static async Task<IActionResult> ProcessGet(string partition, string shipmentId, ILogger logger)
	{
		try
		{
			Shipment shipment = await Storage.GetShipment(partition, shipmentId);
			JObject jsonResult = shipment.ToJson();
			return new OkObjectResult(jsonResult.ToString(Newtonsoft.Json.Formatting.None));
		}
		catch (Azure.RequestFailedException ex)
		{
			logger.LogError($"Failed to get shipment '{partition}/{shipmentId}': {ex}");
			return new StatusCodeResult(ex.Status);
		}
	}

	private static async Task<IActionResult> ProcessGetAll(string partition, string continuationToken, ILogger logger)
	{
		try
		{
			Storage.ShipmentPage shipmentPage = await Storage.GetShipments(partition, continuationToken);
			JObject jsonResult = shipmentPage.ToJson();
			return new OkObjectResult(jsonResult.ToString(Newtonsoft.Json.Formatting.None));
		}
		catch (Azure.RequestFailedException ex)
		{
			logger.LogError($"Failed to get shipments '{partition}/{continuationToken}': {ex}");
			return new StatusCodeResult(ex.Status);
		}
	}

	private static async Task<IActionResult> ProcessPost(string partition, string data, ILogger logger)
	{
		if (string.IsNullOrEmpty(data))
			return new BadRequestObjectResult("Body is required");
		Shipment shipment = Shipment.FromJson(JObject.Parse(data));
		shipment.NewId();
		try
		{
			shipment.PartitionKey = partition;
			Azure.Response storageResponse = await Storage.PostShipment(shipment);
			if (storageResponse.Status == 200)
				return new OkObjectResult(shipment.Id);
			return new StatusCodeResult(storageResponse.Status);
		}
		catch (Azure.RequestFailedException ex)
		{
			logger.LogError($"Failed to post shipment '{partition}/{shipment.Id}': {ex}");
			return new StatusCodeResult(ex.Status);
		}
	}

	private static async Task<IActionResult> ProcessPut(string partition, string data, ILogger logger)
	{
		if (string.IsNullOrEmpty(data))
			return new BadRequestObjectResult("Body is required");
		Shipment shipment = Shipment.FromJson(JObject.Parse(data));
		try
		{
			shipment.PartitionKey = partition;
			Azure.Response storageResponse = await Storage.PutShipment(shipment);
			return new StatusCodeResult(storageResponse.Status);
		}
		catch (Azure.RequestFailedException ex)
		{
			logger.LogError($"Failed to put shipment '{partition}/{shipment.Id}': {ex}");
			return new StatusCodeResult(ex.Status);
		}
	}

	private static async Task<IActionResult> ProcessDelete(string partition, string shipmentId, ILogger logger)
	{
		try
		{
			Azure.Response storageResponse = await Storage.DeleteShipment(partition, shipmentId);
			return new StatusCodeResult(storageResponse.Status);
		}
		catch (Azure.RequestFailedException ex)
		{
			logger.LogError($"Failed to delete shipment '{partition}/{shipmentId}': {ex}");
			return new StatusCodeResult(ex.Status);
		}
	}
}
