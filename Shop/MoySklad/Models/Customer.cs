using System;
using Newtonsoft.Json.Linq;

namespace ShopServices.Shop.MoySkald.Models;

internal class Customer
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Email { get; set; }
	public string Phone { get; set; }
	public string ActualAddress { get; set; }
	public string ActualAddressComment { get; set; }
	public string LegalAddress { get; set; }

	public static Customer FromJson(JObject jobj)
	{
		var actualAddressFull = (JObject)jobj.GetValue("actualAddressFull");
		return new Customer() {
			Id = (string)jobj.GetValue("id"),
			Name = (string)jobj.GetValue("name"),
			Email = (string)jobj.GetValue("email"),
			Phone = (string)jobj.GetValue("phone"),
			ActualAddress = (string)jobj.GetValue("actualAddress"),
			ActualAddressComment = (string)actualAddressFull?.GetValue("comment"),
			LegalAddress = (string)jobj.GetValue("legalAddress"),
		};
	}

	public JObject ToJson()
	{
		return new JObject() {
			{ "id", Id },
			{ "name", Name },
			{ "email", Email },
			{ "phone", Phone },
			{ "actualAddress", ActualAddress },
			{ "actualAddressComment", ActualAddressComment },
			{ "legalAddress", LegalAddress },
		};
	}
}
