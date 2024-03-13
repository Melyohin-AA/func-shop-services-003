using System;
using Newtonsoft.Json.Linq;

namespace ShopServices.Shop.MoySkald.Models;

internal class CustomerOrder
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Code { get; set; }
	public string Description { get; set; }
	public double? Sum { get; set; }
	public string ShippingCompany { get; set; }
	public string StateRef { get; set; }
	public string AgentRef { get; set; }

	public static CustomerOrder FromJson(JObject jobj)
	{
		JObject state = (JObject)jobj.GetValue("state");
		JObject stateMeta = (JObject)state.GetValue("meta");
		JObject agent = (JObject)jobj.GetValue("agent");
		JObject agentMeta = (JObject)agent.GetValue("meta");
		JArray attributes = (JArray)jobj.GetValue("attributes");
		string shippingCompany = null;
		if (attributes != null)
		{
			foreach (JToken attribute in attributes)
			{
				if (attribute is JObject attributeJobj)
				{
					string attributeName = (string)attributeJobj.GetValue("name");
					if (attributeName != "Транспортная компания") continue;
					JObject attributeValue = attributeJobj.GetValue("value") as JObject;
					shippingCompany = (string)attributeValue?.GetValue("name");
					break;
				}
			}
		}
		return new CustomerOrder() {
			Id = (string)jobj.GetValue("id"),
			Name = (string)jobj.GetValue("name"),
			Code = (string)jobj.GetValue("code"),
			Description = (string)jobj.GetValue("description"),
			Sum = (double?)jobj.GetValue("sum"),
			ShippingCompany = shippingCompany,
			StateRef = (string)stateMeta.GetValue("href"),
			AgentRef = (string)agentMeta.GetValue("href"),
		};
	}

	public JObject ToJson()
	{
		return new JObject() {
			{ "id", Id },
			{ "name", Name },
			{ "code", Code },
			{ "description", Description },
			{ "sum", Sum },
			{ "shippingCompany", ShippingCompany },
		};
	}
	public JObject ToJson(CustomerOrderState state)
	{
		return new JObject() {
			{ "id", Id },
			{ "name", Name },
			{ "code", Code },
			{ "description", Description },
			{ "sum", Sum },
			{ "shippingCompany", ShippingCompany },
			{ "state", state?.ToJson() },
		};
	}
}
