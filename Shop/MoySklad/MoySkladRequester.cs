using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ShopServices.Shop.MoySkald;

internal class MoySkladRequester
{
	private readonly string accessToken;
	private readonly ILogger logger;

	public MoySkladRequester(string accessToken, ILogger logger)
	{
		this.accessToken = accessToken;
		this.logger = logger;
	}

	public async Task<string> Fetch(Uri uri)
	{
		byte[] compressedBody;
		using (HttpPool.AllocatedHttp allocatedHttp = await HttpPool.Acquire())
		{
			var request = new HttpRequestMessage(HttpMethod.Get, uri);
			request.Headers.Add("Accept-Encoding", "gzip");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
			HttpResponseMessage response = await allocatedHttp.Http.SendAsync(request);
			if (!response.IsSuccessStatusCode)
			{
				logger.LogError($"Unexpected resposnse code '{(int)response.StatusCode}' from '{uri}'");
				return null;
			}
			compressedBody = await response.Content.ReadAsByteArrayAsync();
		}
		using (MemoryStream compressedBodyStream = new MemoryStream(compressedBody))
		using (GZipStream zipBodyStream = new GZipStream(compressedBodyStream, CompressionMode.Decompress))
		using (MemoryStream decompressedBodyStream = new MemoryStream())
		{
			await zipBodyStream.CopyToAsync(decompressedBodyStream);
			decompressedBodyStream.Position = 0;
			using (var stringReader = new StreamReader(decompressedBodyStream, Encoding.UTF8))
				return await stringReader.ReadToEndAsync();
		}
	}
}
