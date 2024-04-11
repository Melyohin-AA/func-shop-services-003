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
using System.Diagnostics.CodeAnalysis;

namespace ShopServices.Triggers;

internal static class PeriodicBackupTriggers
{
	[FunctionName("HourlyBackupTrigger")]
	public static async Task RunHourly(
		//todo: adjust for timezones
		[TimerTrigger("0 0 12-23,0-2 * * *")] TimerInfo timer,
		// [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/test-bulk-backups-hourly")] HttpRequest request,
		[Blob("https://rgshopservices003b728.blob.core.windows.net/periodic-backup-timestamps/hourly.txt")] Azure.Storage.Blobs.Specialized.BlockBlobClient blob,
		ILogger logger)
	{
		await Run(blob, logger, BackupFrequency.Hourly, Mailing.NotificationReason.ShipmentBackupHourly);
	}
	[FunctionName("DailyBackupTrigger")]
	public static async Task RunDaily(
		//todo: adjust for timezones
		[TimerTrigger("0 0 0 * * *")] TimerInfo timer,
		// [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/test-bulk-backups-daily")] HttpRequest request,
		[Blob("https://rgshopservices003b728.blob.core.windows.net/periodic-backup-timestamps/daily.txt")] Azure.Storage.Blobs.Specialized.BlockBlobClient blob,
		ILogger logger)
	{
		await Run(blob, logger, BackupFrequency.Daily, Mailing.NotificationReason.ShipmentBackupDaily);
	}
	[FunctionName("WeeklyBackupTrigger")]
	public static async Task RunWeekly(
		//todo: adjust for timezones
		[TimerTrigger("0 0 0 * * 6")] TimerInfo timer,
		// [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/test-bulk-backup-weekly")] HttpRequest request,
		[Blob("https://rgshopservices003b728.blob.core.windows.net/periodic-backup-timestamps/weekly.txt")] Azure.Storage.Blobs.Specialized.BlockBlobClient blob,
		ILogger logger)
	{
		await Run(blob, logger, BackupFrequency.Weekly, Mailing.NotificationReason.ShipmentBackupWeekly);
	}
	private static async Task Run(

		Azure.Storage.Blobs.Specialized.BlockBlobClient blob,
		ILogger logger,
		BackupFrequency selectedFrequency,
		Mailing.NotificationReason notificationReason)
	{
		Encoding utf8 = Encoding.UTF8;
		DateTimeOffset now = DateTimeOffset.UtcNow;
		DateTimeOffset ts;
		using (Stream blobRead = await blob.OpenReadAsync())
		{
			byte[] buf = new byte[blobRead.Length];
			await blobRead.ReadAsync(buf, 0, (int)blobRead.Length);
			string text = utf8.GetString(buf);
			if (!long.TryParse(text, out long tslong))
			{
				//todo: change handling
				throw new Exception($"{selectedFrequency} blob is not a valid number");
			}
			ts = DateTimeOffset.FromUnixTimeMilliseconds(tslong);
		}
		Shop.Storing.Storage storage = new("p1", null, logger);
		Mailing.EmailSender email = new(logger);
		Mailing.ShipmentBackupNotifier notifier = new(logger, email);

		List<Shop.Storing.Models.Shipment> allShipments = new();
		string continuationToken = null;
		int status;
		Shop.Storing.Storage.ShipmentPage page;

		//bounded loop in case something breaks really bad to avoid racking up storage debt infinitely
		const int NUM_ITERATIONS = 2048;
		for (int i = 0; i < NUM_ITERATIONS; i++)
		{
			(status, page) = await storage.GetShipmentsModifiedAfter(continuationToken, ts);
			logger.LogDebug($"Page {i} fetched with status {status}");
			allShipments.AddRange(page.Shipments);
			continuationToken = page.ContinuationToken;
			if (continuationToken is null) break;
			if (i == NUM_ITERATIONS - 1)
			{
				logger.LogWarning($"BULK BACKUP HAD A BAD LOOP! Either something broke or there are more than {NUM_ITERATIONS} filtered pages. If it's the latter, update {nameof(PeriodicBackupTriggers)}.");
			}
		}

		if (allShipments.Count == 0)
		{
			logger.LogInformation($"No changes since last backup; skipping");
			return;
		}

		await notifier.SendBackupBulkShipmentsAsync(notificationReason, allShipments);
		try
		{
			using Stream blobWrite = await blob.OpenWriteAsync(true);
			string text = now.ToUnixTimeMilliseconds().ToString();
			await blobWrite.WriteAsync(utf8.GetBytes(text));
		}
		catch (Exception ex)
		{
			logger.LogError($"Error when writing updated {selectedFrequency} timestamp {ex}");
		}
	}

	internal enum BackupFrequency
	{
		Hourly,
		Daily,
		Weekly
	}
}