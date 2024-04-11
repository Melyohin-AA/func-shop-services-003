using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;

namespace ShopServices.Mailing;

internal class ShipmentBackupNotifier
{
	private static readonly string addressToSendNotificationsTo = System.Environment.GetEnvironmentVariable("SHOPSERVICES_BACKUPS_MAIL");
	private readonly ILogger logger;
	private readonly EmailSender email;
	public ShipmentBackupNotifier(ILogger logger, EmailSender email)
	{
		this.logger = logger;
		if (addressToSendNotificationsTo is null or "")
		{
			logger.LogError($"SHOPSERVICES_BACKUPS_MAIL must be set for email notifications to work.");
			logger.LogError($"Notifications will fail.");
			return;
		}
		this.email = email;
	}
	public async Task SendBackupSingleShipmentAsync(
		NotificationReason notificationReason,
		Shop.Storing.Models.Shipment shipment)
	{
		DateTime now = DateTime.UtcNow;
		if (email is null)
		{
			logger.LogError($"Cannot send notification for {notificationReason}, {shipment.Id}: Notifier not initialized");
			return;
		}
		EmailTextBuilder textBuilder = new();
		textBuilder
			.AppendLine($"This is a notification for: {notificationReason}")
			.AppendTableStart()
			.AppendShipmentTableHeader()
			.AppendShipmentRecord(shipment)
			.AppendTableEnd();
		await email.SendSingleAsync(
			addressToSendNotificationsTo,
			$"{now:yymmdd-hhmmss}-{now.Millisecond}-{StringifyReasonForTopic(notificationReason)}-{shipment.Id}",
			textBuilder.BuildPlainText(),
			textBuilder.BuildHTML(),
			true
		);
	}
	public async Task SendBackupBulkShipmentsAsync(
		NotificationReason notificationReason,
		IEnumerable<Shop.Storing.Models.Shipment> shipments)
	{
		DateTime now = DateTime.UtcNow;
		if (email is null)
		{
			logger.LogError($"Cannot send bulk notification for {notificationReason}: Notifier not initialized");
			return;
		}
		EmailTextBuilder textBuilder = new();
		textBuilder
			.AppendLine($"This is a notification for: {notificationReason}")
			.AppendTableStart()
			.AppendShipmentTableHeader();
		foreach (Shop.Storing.Models.Shipment shipment in shipments)
		{
			textBuilder.AppendShipmentRecord(shipment);
		}
		textBuilder.AppendTableEnd();
		await email.SendSingleAsync(
			addressToSendNotificationsTo,
			$"{now:yymmdd-hhmmss}-{now.Millisecond}-{StringifyReasonForTopic(notificationReason)}",
			textBuilder.BuildPlainText(),
			textBuilder.BuildHTML(),
			true
		);
	}
	private static string StringifyReasonForTopic(NotificationReason reason) => reason switch
	{
		NotificationReason.ShipmentCreated => "new",
		NotificationReason.ShipmentUpdated => "upd",
		NotificationReason.ShipmentDeleted => "del",
		NotificationReason.ShipmentBackupHourly => "hourly",
		NotificationReason.ShipmentBackupDaily => "daily",
		NotificationReason.ShipmentBackupWeekly => "weekly",
		_ => ((int)reason).ToString()
	}
}

