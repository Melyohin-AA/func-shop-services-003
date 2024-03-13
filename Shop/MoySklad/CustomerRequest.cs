using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;

namespace ShopServices.Shop.MoySkald;

internal class CustomerRequest
{
	private readonly MoySkladRequester requester;

	public string Response { get; private set; }
	public JObject JResponse { get; private set; }
	public int CustomerCount { get; private set; }
	public Models.Customer Customer { get; private set; }

	public CustomerRequest(MoySkladRequester requester)
	{
		this.requester = requester;
	}

	public async Task ExecuteByRef(string href)
	{
		var uri = new Uri(href);
		Response = await requester.Fetch(uri);
		JResponse = JObject.Parse(Response);
		if (JResponse.ContainsKey("id"))
		{
			CustomerCount = 1;
			Customer = Models.Customer.FromJson(JResponse);
		}
		else
		{
			CustomerCount = 0;
			Customer = null;
		}
	}

	public async Task ExecuteByEmail(string email)
	{
		const string url = "https://api.moysklad.ru/api/remap/1.2/entity/counterparty";
		var uri = new Uri(QueryHelpers.AddQueryString(url, "filter", $"email={email}"));
		Response = await requester.Fetch(uri);
		JResponse = JObject.Parse(Response);
		JArray foundCustomers = (JArray)JResponse.GetValue("rows");
		CustomerCount = foundCustomers.Count;
		if (CustomerCount > 0)
		{
			JObject customerJobj = (JObject)foundCustomers.First;
			Customer = Models.Customer.FromJson(customerJobj);
		}
		else Customer = null;
	}
}
