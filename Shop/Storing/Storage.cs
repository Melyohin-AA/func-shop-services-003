using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using ShopServices.Shop.Storing.Models;

namespace ShopServices.Shop.Storing;

// TODO: Cover 'Storage' methods with unit tests

internal class Storage
{
	private static readonly TableServiceClient shipmentServiceClient;
	private static readonly TableClient shipmentTable;
	private static readonly TableServiceClient shipmentLockServiceClient;
	private static readonly TableClient shipmentLockTable;

	static Storage()
	{
		TokenCredential cred = new DefaultAzureCredential();
		// TODO: Replace with connection strings
		string shipmentTableUri = Environment.GetEnvironmentVariable("SHIPMENT_TABLE_URI");
		shipmentServiceClient = new TableServiceClient(new Uri(shipmentTableUri), cred);
		shipmentTable = shipmentServiceClient.GetTableClient("Shipments");
		string shipmentLockTableUri = Environment.GetEnvironmentVariable("SHIPMENT_LOCK_TABLE_URI");
		shipmentLockServiceClient = new TableServiceClient(new Uri(shipmentLockTableUri), cred);
		shipmentLockTable = shipmentLockServiceClient.GetTableClient("ShipmentLocks");
	}

	public string Partition { get; }
	public string DeviceId { get; }
	public ILogger Logger { get; }

	public Storage(string partition, string deviceId, ILogger logger)
	{
		Partition = partition;
		DeviceId = deviceId;
		Logger = logger;
	}

	public async Task<(int, ShipmentPage)> GetShipments(string continuationToken)
	{
		string filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Partition);
		try
		{
			AsyncPageable<Shipment> shipments = shipmentTable.QueryAsync<Shipment>(filter: filter, maxPerPage: 256);
			await foreach (Page<Shipment> page in shipments.AsPages(continuationToken))
				return (200, new ShipmentPage(page));
		}
		catch (RequestFailedException ex)
		{
			Logger.LogError($"Failed to get shipments '{continuationToken}': {ex}");
			return (ex.Status, new ShipmentPage());
		}
		return (200, new ShipmentPage());
	}

	public async Task<(int, Shipment)> GetShipment(string id)
	{
		try
		{
			return (200, await shipmentTable.GetEntityAsync<Shipment>(Partition, id));
		}
		catch (RequestFailedException ex)
		{
			Logger.LogError($"Failed to get shipment '{id}': {ex}");
			return (ex.Status, null);
		}
	}

	public async Task<int> PostShipment(Shipment shipment)
	{
		shipment.PartitionKey = Partition;
		shipment.LastModTS = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		return await MakeRequest(shipmentTable.AddEntityAsync(shipment));
	}

	public async Task<int> PutShipment(Shipment shipment, bool releaseLock)
	{
		Shipment former = await MakeRequest(shipmentTable.GetEntityIfExistsAsync<Shipment>(Partition, shipment.Id));
		if ((shipment == null) || (shipment.LastModTS != former.LastModTS))
			return 409;
		ShipmentLock shLock =
			await MakeRequest(shipmentLockTable.GetEntityIfExistsAsync<ShipmentLock>(Partition, shipment.Id));
		DateTimeOffset now = DateTimeOffset.UtcNow;
		if (shLock?.IsLockedNotBy(DeviceId, now) ?? false)
			return 403;
		shipment.PartitionKey = Partition;
		shipment.LastModTS = now.ToUnixTimeMilliseconds();
		int code = await MakeRequest(shipmentTable.UpdateEntityAsync(shipment, former.ETag, TableUpdateMode.Replace));
		if (code != 204)
			return code;
		if (shLock != null)
		{
			if (releaseLock || !shLock.IsLocked(now))
				return await MakeRequest(shipmentLockTable.DeleteEntityAsync(Partition, shipment.Id, shLock.ETag));
			// Renewing the shipment lock if it is locked by the current device
			int updateLockCode = await MakeRequest(shipmentLockTable.UpdateEntityAsync(shLock, shLock.ETag));
			if (updateLockCode != 204)
				Logger.LogError($"Failed to update shipment lock '{shipment.Id}': code {updateLockCode}");
		}
		return 204;
	}

	public async Task<int> DeleteShipment(string id)
	{
		ShipmentLock shLock =
			await MakeRequest(shipmentLockTable.GetEntityIfExistsAsync<ShipmentLock>(Partition, id));
		(int acquireLockCode, Shipment shipment) = await AcquireShipmentLock(id, shLock);
		if (acquireLockCode != 204)
			return acquireLockCode;
		int deleteShipmentCode = await MakeRequest(shipmentTable.DeleteEntityAsync(Partition, id, shipment.ETag));
		if ((deleteShipmentCode == 204) || (shLock == null))
		{
			int releaseLockCode = await ReleaseShipmentLock(id);
			if (releaseLockCode != 204)
				Logger.LogError($"Failed to release shipment lock '{id}': code {releaseLockCode}");
		}
		return deleteShipmentCode;
	}

	public async Task<int> AcquireShipmentLock(string shipmentId)
	{
		ShipmentLock shLock =
			await MakeRequest(shipmentLockTable.GetEntityIfExistsAsync<ShipmentLock>(Partition, shipmentId));
		(int code, Shipment _) = await AcquireShipmentLock(shipmentId, shLock);
		return code;
	}
	private async Task<(int, Shipment)> AcquireShipmentLock(string shipmentId, ShipmentLock shLock)
	{
		int code;
		if (shLock != null)
		{
			DateTimeOffset now = DateTimeOffset.UtcNow;
			if (shLock.IsLockedNotBy(DeviceId, now))
				return (403, null);
			shLock.EditorId = DeviceId;
			code = await MakeRequest(shipmentLockTable.UpdateEntityAsync(shLock, shLock.ETag));
		}
		else
		{
			shLock = new ShipmentLock() {
				PartitionKey = Partition,
				ShipmentId = shipmentId,
				EditorId = DeviceId,
			};
			code = await MakeRequest(shipmentLockTable.AddEntityAsync(shLock));
		}
		if (code != 204)
			return (code, null);
		Shipment shipment = await MakeRequest(shipmentTable.GetEntityIfExistsAsync<Shipment>(Partition, shipmentId));
		if (shipment != null)
			return (204, shipment);
		// Preventing locking an inexistent shipment
		int deleteShipmentCode =
			await MakeRequest(shipmentLockTable.DeleteEntityAsync(Partition, shipmentId, shLock.ETag));
		if (deleteShipmentCode != 204)
			Logger.LogError($"Failed to delete shipment lock '{shipmentId}' due to inexistence of its shipment");
		return (400, null);
	}

	public async Task<int> ReleaseShipmentLock(string shipmentId)
	{
		ShipmentLock shLock =
			await MakeRequest(shipmentLockTable.GetEntityIfExistsAsync<ShipmentLock>(Partition, shipmentId));
		if (shLock == null)
			return 204;
		DateTimeOffset now = DateTimeOffset.UtcNow;
		if (shLock.IsLockedNotBy(DeviceId, now))
			return 403;
		int deleteCode = await MakeRequest(shipmentLockTable.DeleteEntityAsync(Partition, shipmentId, shLock.ETag));
		return (deleteCode != 404) ? deleteCode : 204;
	}

	private async Task<int> MakeRequest(Task<Response> task)
	{
		try
		{
			Response response = await task;
			return response.Status;
		}
		catch (RequestFailedException ex)
		{
			Logger.LogError(ex.ToString());
			return ex.Status;
		}
	}
	private async Task<T> MakeRequest<T>(Task<NullableResponse<T>> task) where T : class
	{
		try
		{
			NullableResponse<T> response = await task;
			return response.HasValue ? response.Value : null;
		}
		catch (RequestFailedException ex)
		{
			Logger.LogError(ex.ToString());
			return null;
		}
	}

	public struct ShipmentPage
	{
		public string ContinuationToken { get; }
		public Shipment[] Shipments { get; }

		public ShipmentPage(string continuationToken, Shipment[] shipments)
		{
			ContinuationToken = continuationToken;
			Shipments = shipments;
		}
		public ShipmentPage(Page<Shipment> azPage)
		{
			ContinuationToken = azPage.ContinuationToken;
			Shipments = azPage.Values.ToArray();
		}

		public JObject ToJson()
		{
			return new JObject() {
				{ "continuationToken", ContinuationToken },
				{ "shipments", new JArray(Shipments.Select(sh => sh.ToJson())) },
			};
		}
	}
}
