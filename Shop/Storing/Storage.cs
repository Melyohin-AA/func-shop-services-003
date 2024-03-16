using System;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using ShopServices.Shop.Storing.Models;

namespace ShopServices.Shop.Storing;

internal class Storage
{
	//~ Change to connection string
	public static readonly string tableUri = Environment.GetEnvironmentVariable("STORING_TABLE_URI");

	private static readonly object serviceClientLock = new object();
	private static TableServiceClient serviceClient;
	private static TableServiceClient ServiceClient
	{
		get
		{
			if (serviceClient == null)
			{
				lock (serviceClientLock)
				{
					if (serviceClient == null)
					{
						TokenCredential cred = new DefaultAzureCredential();
						serviceClient = new TableServiceClient(new Uri(tableUri), cred);
					}
				}
			}
			return serviceClient;
		}
	}

	public static TableClient NewShipmentsTableClient()
	{
		return ServiceClient.GetTableClient("Shipments");
	}

	public static async Task<ShipmentPage> GetShipments(string partition, string continuationToken)
	{
		TableClient tableClient = NewShipmentsTableClient();
		string filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition);
		AsyncPageable<Shipment> shipments = tableClient.QueryAsync<Shipment>(filter: filter, maxPerPage: 256);
		await foreach (Page<Shipment> page in shipments.AsPages(continuationToken))
			return new ShipmentPage(page);
		return new ShipmentPage();
	}

	public static async Task<Response<Shipment>> GetShipment(string partition, string id)
	{
		TableClient tableClient = NewShipmentsTableClient();
		return await tableClient.GetEntityAsync<Shipment>(partition, id);
	}

	public static async Task<Response> PostShipment(Shipment shipment)
	{
		shipment.LastModTS = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		TableClient tableClient = NewShipmentsTableClient();
		return await tableClient.AddEntityAsync(shipment);
	}

	public static async Task<(Response, Shipment)> PutShipment(Shipment shipment, string deviceId, bool releaseModLock)
	{
		TableClient tableClient = NewShipmentsTableClient();
		Shipment former = await tableClient.GetEntityAsync<Shipment>(shipment.PartitionKey, shipment.Id);
		long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		if (((deviceId != former.EditorId) && former.IsModLocked(now)) || (now != former.LastModTS))
			return (null, former);
		shipment.LastModTS = now;
		if (releaseModLock)
			shipment.EditorId = null;
		Response response = await tableClient.UpdateEntityAsync(shipment, former.ETag, TableUpdateMode.Replace);
		if (response.Status / 100 != 2)
			return (response, former);
		return (response, shipment);
	}

	public static async Task<Response> DeleteShipment(string partition, string id, ETag ifMatch = default)
	{
		TableClient tableClient = NewShipmentsTableClient();
		return await tableClient.DeleteEntityAsync(partition, id, ifMatch);
	}

	public static async Task<Shipment> AcquireShipmentModLock(string partition, string id, string deviceId)
	{
		TableClient tableClient = NewShipmentsTableClient();
		for (byte attempt = 0; attempt < 4; attempt++)
		{
			Shipment shipment = await tableClient.GetEntityAsync<Shipment>(partition, id);
			long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			if (shipment.IsModLocked(now))
				return shipment;
			shipment.EditorId = deviceId;
			shipment.ModLockTS = now;
			Response response = await tableClient.UpdateEntityAsync(shipment, shipment.ETag, TableUpdateMode.Replace);
			if (response.Status / 100 == 2)
				return shipment;
		}
		return null;
	}

	public static async Task ReleaseShipmentModLock(string partition, string id, string deviceId)
	{
		TableClient tableClient = NewShipmentsTableClient();
		Shipment shipment = await tableClient.GetEntityAsync<Shipment>(partition, id);
		long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		if (!shipment.IsModLockedBy(deviceId, now)) return;
		shipment.EditorId = null;
		Response response = await tableClient.UpdateEntityAsync(shipment, shipment.ETag, mode: TableUpdateMode.Replace);
		if (response.Status / 100 != 2)
			throw new Exception($"Failed to release shipment '{partition}/{id}' modlock");
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
