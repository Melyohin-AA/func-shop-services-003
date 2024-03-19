using System;
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

internal static class ShopMoyskladCustomerOrderApiTrigger
{
	public const string Route = "api/shop/moysklad/customer-order";

	[FunctionName(nameof(ShopMoyskladCustomerOrderApiTrigger))]
	public static async Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Route)] HttpRequest request,
		ILogger logger)
	{
		if (!Authorization.SimpleAccess.Check(request))
		{
			logger.LogInformation("Failed to auth");
			return new UnauthorizedResult();
		}
		string code = request.Query["code"];
		if (string.IsNullOrEmpty(code))
			return new BadRequestObjectResult("'code' parameter is required");
		string moyskladAccessToken = Environment.GetEnvironmentVariable("MOYSKLAD_TOKEN");
		var requester = new MoySkladRequester(moyskladAccessToken, logger);
		var customerOrderRequest = new CustomerOrderRequest(requester);
		await customerOrderRequest.ExecuteByCode(code);
		JObject responseJson;
		if (customerOrderRequest.CustomerOrders.Length != 1)
		{
			string msg = $"Found {customerOrderRequest.CustomerOrders.Length} " +
				$"customer orders with '{code}' code, expected 1!";
			logger.LogWarning(msg);
			responseJson = FormJsonResult(customerOrderRequest.CustomerOrders.Length, null, null);
		}
		else
		{
			CustomerOrder customerOrder = customerOrderRequest.CustomerOrders[0];
			var customerRequest = new CustomerRequest(requester);
			await customerRequest.ExecuteByRef(customerOrder.AgentRef);
			responseJson = FormJsonResult(1, customerOrder, customerRequest.Customer);
		}
		return new OkObjectResult(responseJson.ToString(Newtonsoft.Json.Formatting.Indented));
	}

	private static JObject FormJsonResult(int customerOrdersFound, CustomerOrder customerOrder, Customer customer)
	{
		JObject responseJson = new JObject() {
			{ "customerOrdersFound", customerOrdersFound },
			{ "customerOrder", (customerOrdersFound == 1) ? customerOrder.ToJson() : null },
			{ "customer", customer?.ToJson() },
		};
		return responseJson;
	}
}
