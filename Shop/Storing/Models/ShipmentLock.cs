using System;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json.Linq;

namespace ShopServices.Shop.Storing.Models;

internal class ShipmentLock : ITableEntity
{
	public const double LockResetInterval = 30 * 60; // in seconds

	public string PartitionKey { get; set; }
	public string RowKey { get; set; }
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	/*
	shipmentId
	editorId
	*/

	[IgnoreDataMember]
	public string ShipmentId { get => RowKey; set => RowKey = value; }
	public string EditorId { get; set; }

	public bool IsLocked(DateTimeOffset now)
	{
		return Timestamp.HasValue && (now < Timestamp.Value.AddSeconds(LockResetInterval));
	}
	public bool IsLockedBy(string deviceId, DateTimeOffset now)
	{
		return (EditorId == deviceId) && IsLocked(now);
	}
	public bool IsLockedNotBy(string deviceId, DateTimeOffset now)
	{
		return (EditorId != deviceId) && IsLocked(now);
	}

	public static ShipmentLock FromJson(JObject jobj)
	{
		return new ShipmentLock() {
			ShipmentId = (string)jobj.GetValue("shipmentId"),
			EditorId = (string)jobj.GetValue("editorId"),
		};
	}

	public JObject ToJson()
	{
		return new JObject() {
			{ "shipmentId", ShipmentId },
			{ "editorId", EditorId },
		};
	}
}
