using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using ShopServices.Authorization;

namespace ShopServices.Triggers;

internal static class AuthorizationTrigger
{
	[FunctionName(nameof(AuthorizationTrigger))]
	public static Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authorize")] HttpRequest request,
		ILogger logger)
	{
		string sakey = SimpleAccess.GetKeyFromRequest(request);
		return Task.FromResult(AuthorizationGui.Get());
	}
}
