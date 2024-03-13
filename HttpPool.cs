using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ShopServices;

internal static class HttpPool
{
	public const byte Limit = 16;

	private static readonly Dictionary<HttpClient, bool> pool = new Dictionary<HttpClient, bool>();
	private static readonly SemaphoreSlim sema = new SemaphoreSlim(Limit);

	public static async Task<AllocatedHttp> Acquire()
	{
		await sema.WaitAsync();
		lock (pool)
		{
			foreach (var pair in pool)
			{
				if (!pair.Value)
				{
					pool[pair.Key] = true;
					return new AllocatedHttp(pair.Key);
				}
			}
			var http = new HttpClient(new SocketsHttpHandler() { MaxConnectionsPerServer = 1 });
			pool.Add(http, true);
			return new AllocatedHttp(http);
		}
	}

	private static void Release(AllocatedHttp allocatedHttp)
	{
		lock (pool)
		{
			if (!pool[allocatedHttp.Http])
				throw new Exception("Attempt to release not occupied HttpClient!");
			pool[allocatedHttp.Http] = false;
		}
		sema.Release();
	}

	public class AllocatedHttp : IDisposable
	{
		public HttpClient Http { get; }
		public bool Released { get; private set; }

		public AllocatedHttp(HttpClient http)
		{
			Http = http;
		}

		public void Dispose()
		{
			lock (this)
			{
				if (Released) return;
				Release(this);
				Released = true;
			}
		}
	}
}
