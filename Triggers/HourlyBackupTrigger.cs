using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Azure;
using System.Text;

namespace ShopServices.Triggers;

internal static class HourlyBackupTrigger
{
	[FunctionName(nameof(HourlyBackupTrigger))]
	public static async Task Run(
		//todo: adjust for timezones
		[TimerTrigger("0 0 9-23 * * *")] TimerInfo timer,
		// [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/test-bulk-backups")] HttpRequest request,
		// blobs can't be opened for read+write apparently
		[Blob("https://rgshopservices003b728.blob.core.windows.net/periodic-backup-timestamps/hourly.txt", FileAccess.Read)] Stream blobRead,
		ILogger logger)
	{
		System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
		byte[] buf = new byte[blobRead.Length];
		await blobRead.ReadAsync(buf, 0, (int)blobRead.Length);
		string text = utf8.GetString(buf);
		if (!long.TryParse(text, out long tslong))
		{
			//todo: change handling
			throw new Exception("Hourly blob is not a valid number");
		}
		DateTimeOffset ts = DateTimeOffset.FromUnixTimeMilliseconds(tslong);
		DateTimeOffset now = DateTimeOffset.UtcNow;
		Shop.Storing.Storage storage = new("p1", "0000", logger);
		Mailing.EmailSender email = new(logger);
		Mailing.ShipmentBackupNotifier notifier = new(logger, email);

		List<Shop.Storing.Models.Shipment> allShipments = new();
		string continuationToken = "1";
		int status;
		Shop.Storing.Storage.ShipmentPage page;
		(status, page) = await storage.GetShipmentsModifiedAfter(continuationToken, ts);
		logger.LogWarning($"{status}, {page.Shipments?.Length}");
		allShipments.AddRange(page.Shipments);
		await notifier.SendBackupBulkShipmentsAsync(Mailing.NotificationReason.ShipmentBackupHourly, allShipments);
	}
	internal enum BackupFrequency
	{
		Hourly,
		Daily,
		Weekly
	}
}