using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ShopServices.Shop.MoySkald;
using ShopServices.Shop.MoySkald.Models;

namespace ShopServices.Triggers;

internal static class ShopCustomerOrdersApiTrigger
{
	public const int FormatVersion = 1;

	[FunctionName(nameof(ShopCustomerOrdersApiTrigger))]
	public static async Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/shop/customer-orders")] HttpRequest request,
		ILogger logger)
	{
		if (!Authorization.SimpleAccess.Check(request))
		{
			logger.LogInformation("Failed to auth");
			return new UnauthorizedResult();
		}
		string email = request.Query["email"];
		string moyskladUn = Environment.GetEnvironmentVariable("MOYSKLAD_UN") ?? request.Query["un"];
		string moyskladPw = Environment.GetEnvironmentVariable("MOYSKLAD_PW") ?? request.Query["pw"];
		var requester = new MoySkladRequester(moyskladUn, moyskladPw, logger);
		var customerRequest = new CustomerRequest(requester);
		await customerRequest.ExecuteByEmail(email);
		JObject responseJson;
		if (customerRequest.CustomerCount != 1)
		{
			string msg = $"Found {customerRequest.CustomerCount} customers with '{email}' email, expected 1!";
			logger.LogWarning(msg);
			responseJson = FormJsonResult(customerRequest.CustomerCount, null, null, null);
		}
		else
		{
			var customerOrderRequest = new CustomerOrderRequest(requester);
			await customerOrderRequest.ExecuteByCustomerId(customerRequest.Customer.Id);
			var customerOrderStateRequests = new Dictionary<string, CustomerOrderStateRequest>();
			var customerOrderStateRequestTasks = new List<Task>(customerOrderRequest.CustomerOrders.Length);
			foreach (CustomerOrder customerOrder in customerOrderRequest.CustomerOrders)
			{
				var customerOrderStateRequest = new CustomerOrderStateRequest(requester);
				customerOrderStateRequests.Add(customerOrder.Id, customerOrderStateRequest);
				customerOrderStateRequestTasks.Add(customerOrderStateRequest.Execute(customerOrder.StateRef));
			}
			await Task.WhenAll(customerOrderStateRequestTasks);
			responseJson = FormJsonResult(1, customerRequest.Customer,
				customerOrderRequest.CustomerOrders, customerOrderStateRequests);
		}
		return new OkObjectResult(responseJson.ToString(Newtonsoft.Json.Formatting.None));
	}

	private static JObject FormJsonResult(int customersFound, Customer customer, CustomerOrder[] customerOrders,
		Dictionary<string, CustomerOrderStateRequest> customerOrderStateRequests)
	{
		JObject responseJson = new JObject() {
			{ "formatVersion", FormatVersion },
			{ "customersFound", customersFound },
		};
		if (customersFound != 1)
		{
			responseJson["customer"] = null;
			responseJson["customerOrders"] = null;
			return responseJson;
		}
		responseJson["customer"] = customer.ToJson();
		var customerOrderJarr = new JArray();
		foreach (CustomerOrder customerOrder in customerOrders)
		{
			CustomerOrderState state = customerOrderStateRequests[customerOrder.Id].State;
			customerOrderJarr.Add(customerOrder.ToJson(state));
		}
		responseJson["customerOrders"] = customerOrderJarr;
		return responseJson;
	}
}
