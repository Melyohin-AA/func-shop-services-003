using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace ShopServices.Shop.MoySkald;

internal class CustomerOrderRequest
{
	private readonly MoySkladRequester requester;

	public string Response { get; private set; }
	public JObject JResponse { get; private set; }
	public Models.CustomerOrder[] CustomerOrders { get; private set; }

	public CustomerOrderRequest(MoySkladRequester requester)
	{
		this.requester = requester;
	}

	public async Task ExecuteByCustomerId(string customerId)
	{
		const string url = "https://api.moysklad.ru/api/remap/1.2/entity/customerorder";
		string filter = $"agent=https://api.moysklad.ru/api/remap/1.2/entity/counterparty/{customerId}";
		var uri = new Uri(QueryHelpers.AddQueryString(url, "filter", filter));
		Response = await requester.Fetch(uri);
		JResponse = JObject.Parse(Response);
		JArray foundOrders = (JArray)JResponse.GetValue("rows");
		CustomerOrders = new Models.CustomerOrder[foundOrders.Count];
		int i = 0;
		foreach (JToken foundOrder in foundOrders)
			CustomerOrders[i++] = Models.CustomerOrder.FromJson((JObject)foundOrder);
	}

	public async Task ExecuteByCode(string code)
	{
		const string url = "https://api.moysklad.ru/api/remap/1.2/entity/customerorder";
		string filter = (IsWebCustomerOrderCode(code) ? "code=" : "name=") + code;
		var uri = new Uri(QueryHelpers.AddQueryString(url, "filter", filter));
		Response = await requester.Fetch(uri);
		JResponse = JObject.Parse(Response);
		JArray foundOrders = (JArray)JResponse.GetValue("rows");
		CustomerOrders = new Models.CustomerOrder[foundOrders.Count];
		int i = 0;
		foreach (JToken foundOrder in foundOrders)
			CustomerOrders[i++] = Models.CustomerOrder.FromJson((JObject)foundOrder);
	}
	private static bool IsWebCustomerOrderCode(string code)
	{
		if ((code.Length == 7) && code.StartsWith("60"))
		{
			for (byte i = 2; i < 7; i++)
				if (!char.IsDigit(code[i]))
					return false;
			return true;
		}
		return false;
	}
}
