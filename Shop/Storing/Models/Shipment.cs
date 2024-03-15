using System;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json.Linq;

namespace ShopServices.Shop.Storing.Models;

internal class Shipment : ITableEntity
{
	public string PartitionKey { get; set; }
	public string RowKey { get => Id; set => Id = value; }
	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }

	/*
	id
	group
	state
	trackCode
	orderIds
	customerName
	customerPhone
	customerEmail
	receiverName
	receiverPhone
	deliveryCountry
	deliveryCity
	deliveryAddress
	shippingCompany
	comments
	moyskladData
	lastModTS
	*/

	public string Id { get; set; }
	public int Group { get; set; }
	public int State { get; set; }
	public string TrackCode { get; set; }
	public string OrderIds { get; set; }
	public string CustomerName { get; set; }
	public string CustomerPhone { get; set; }
	public string CustomerEmail { get; set; }
	public string ReceiverName { get; set; }
	public string ReceiverPhone { get; set; }
	public string DeliveryCountry { get; set; }
	public string DeliveryCity { get; set; }
	public string DeliveryAddress { get; set; }
	public string ShippingCompany { get; set; }
	public string Comments { get; set; }
	public string MoyskladData { get; set; }
	public long LastModTS { get; set; }

	public static Shipment FromJson(JObject jobj)
	{
		JArray commentsJarr = jobj.GetValue("comments") as JArray;
		string comments = commentsJarr?.ToString(Newtonsoft.Json.Formatting.None);
		JObject moyskladDataJobj = jobj.GetValue("moyskladData") as JObject;
		string moyskladData = moyskladDataJobj?.ToString(Newtonsoft.Json.Formatting.None);
		return new Shipment() {
			Id = (string)jobj.GetValue("id"),
			Group = (int)jobj.GetValue("group"),
			State = (int)jobj.GetValue("state"),
			TrackCode = (string)jobj.GetValue("trackCode"),
			OrderIds = (string)jobj.GetValue("orderIds"),
			CustomerName = (string)jobj.GetValue("customerName"),
			CustomerPhone = (string)jobj.GetValue("customerPhone"),
			CustomerEmail = (string)jobj.GetValue("customerEmail"),
			ReceiverName = (string)jobj.GetValue("receiverName"),
			ReceiverPhone = (string)jobj.GetValue("receiverPhone"),
			DeliveryCountry = (string)jobj.GetValue("deliveryCountry"),
			DeliveryCity = (string)jobj.GetValue("deliveryCity"),
			DeliveryAddress = (string)jobj.GetValue("deliveryAddress"),
			ShippingCompany = (string)jobj.GetValue("shippingCompany"),
			Comments = comments,
			MoyskladData = moyskladData,
		};
	}

	public JObject ToJson()
	{
		return new JObject() {
			{ "id", Id },
			{ "group", Group },
			{ "state", State },
			{ "trackCode", TrackCode },
			{ "orderIds", OrderIds },
			{ "customerName", CustomerName },
			{ "customerPhone", CustomerPhone },
			{ "customerEmail", CustomerEmail },
			{ "receiverName", ReceiverName },
			{ "receiverPhone", ReceiverPhone },
			{ "deliveryCountry", DeliveryCountry },
			{ "deliveryCity", DeliveryCity },
			{ "deliveryAddress", DeliveryAddress },
			{ "shippingCompany", ShippingCompany },
			{ "comments", (Comments != null) ? JArray.Parse(Comments) : null },
			{ "moyskladData", (MoyskladData != null) ? JObject.Parse(MoyskladData) : null },
			{ "lastModTS", LastModTS },
		};
	}

	public void NewId()
	{
		var now = DateTime.UtcNow.AddHours(3.0);
		char year = IntToAlpha(now.Year - 2001);
		char month = IntToAlpha(now.Month - 1);
		string day = now.Day.ToString("D2");
		char hour = IntToAlpha(now.Hour);
		string minute = now.Minute.ToString("D2");
		string second = now.Second.ToString("D2");
		char third = IntToAlpha(now.Millisecond / 39);
		Id = $"{year}{month}{day}{hour}{minute}{second}{third}";
	}
	private char IntToAlpha(int num)
	{
		return (char)(num % 26 + 65);
	}
}
