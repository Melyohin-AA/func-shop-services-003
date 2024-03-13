using System;
using System.Collections.Generic;
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

	public static async Task<Shipment> GetShipment(string partition, string id)
	{
		TableClient tableClient = NewShipmentsTableClient();
		return await tableClient.GetEntityAsync<Shipment>(partition, id);
	}

	public static async Task<Response> PostShipment(Shipment shipment)
	{
		TableClient tableClient = NewShipmentsTableClient();
		return await tableClient.AddEntityAsync(shipment);
	}

	public static async Task<Response> PutShipment(Shipment shipment)
	{
		TableClient tableClient = NewShipmentsTableClient();
		return await tableClient.UpsertEntityAsync(shipment, TableUpdateMode.Replace);
	}

	public static async Task<Response> DeleteShipment(string partition, string id, ETag ifMatch = default)
	{
		TableClient tableClient = NewShipmentsTableClient();
		return await tableClient.DeleteEntityAsync(partition, id, ifMatch);
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
