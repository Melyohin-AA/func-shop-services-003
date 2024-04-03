using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ShopServices;

internal class ShipmentBackupNotifier
{
	private readonly string addressToSendNotificationsTo;
	private readonly ILogger logger;
	private EmailSender email;
	public ShipmentBackupNotifier(ILogger logger, EmailSender email)
	{
		this.email = email;
		this.logger = logger;
	}
	public async Task SendBackupSingleShipmentAsync(
		NotificationReason notificationReason,
		ShopServices.Shop.Storing.Models.Shipment shipment)
	{

		EmailTextBuilder textBuilder = new();
		textBuilder
			.AppendLine($"This is a notification for: {notificationReason}")
			.AppendTableStart()
			.AppendShipmentTableHeader()
			.AppendShipmentRecord(shipment)
			.AppendTableEnd();
		await email.SendSingleAsync(addressToSendNotificationsTo, notificationReason.ToString(), textBuilder.BuildPlainText(), textBuilder.BuildHTML());
	}
}

