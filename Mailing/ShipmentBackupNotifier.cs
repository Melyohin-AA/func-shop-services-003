using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ShopServices.Mailing;

internal class ShipmentBackupNotifier
{
	private static readonly string addressToSendNotificationsTo = System.Environment.GetEnvironmentVariable("SHOPSERVICES_BACKUPS_MAIL");
	private readonly ILogger logger;
	private EmailSender email;
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
		ShopServices.Shop.Storing.Models.Shipment shipment)
	{
		if (email is null)
		{
			logger.LogError($"Cannot send notification for {notificationReason}, {shipment.Id}: Notifier not initialized");
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
			notificationReason.ToString(),
			textBuilder.BuildPlainText(),
			textBuilder.BuildHTML(),
			true
		);
	}
}

