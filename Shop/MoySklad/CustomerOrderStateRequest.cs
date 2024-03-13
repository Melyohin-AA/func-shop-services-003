using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ShopServices.Shop.MoySkald;

internal class CustomerOrderStateRequest
{
	private readonly MoySkladRequester requester;

	public string Response { get; private set; }
	public JObject JResponse { get; private set; }
	public Models.CustomerOrderState State { get; private set; }

	public CustomerOrderStateRequest(MoySkladRequester requester)
	{
		this.requester = requester;
	}

	public async Task Execute(string customerOrderRef)
	{
		Response = await requester.Fetch(new Uri(customerOrderRef));
		JResponse = JObject.Parse(Response);
		State = Models.CustomerOrderState.FromJson(JResponse);
	}
}
