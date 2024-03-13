using System;
using Newtonsoft.Json.Linq;

namespace ShopServices.Shop.MoySkald.Models;

internal class CustomerOrderState
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string StateType { get; set; }

	public static CustomerOrderState FromJson(JObject jobj)
	{
		return new CustomerOrderState() {
			Id = (string)(JValue)jobj.GetValue("id"),
			Name = (string)(JValue)jobj.GetValue("name"),
			StateType = (string)(JValue)jobj.GetValue("stateType"),
		};
	}

	public JObject ToJson()
	{
		return new JObject() {
			{ "id", Id },
			{ "name", Name },
			{ "stateType", StateType },
		};
	}
}
