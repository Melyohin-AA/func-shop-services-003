using HtmlAgilityPack;

namespace ShopServices.Mailing;

/// <summary>
/// Constructs "<b>"th HTML and plaintext message "<b>"dies for a notification email
/// </summary>
internal class EmailTextBuilder
{
	private static readonly string[] columnNames = {
		"id",
		"group",
		"state",
		"trackCode",
		"orderIds",
		"customerName",
		"customerPhone",
		"customerEmail",
		"receiverName",
		"receiverPhone",
		"deliveryCountry",
		"deliveryCity",
		"deliveryAddress",
		"shippingCompany",
		"comments",
	};
	private static readonly string[] properties = {
		shipment.Id,
		shipment.Group.ToString(),
		shipment.State.ToString(),
		shipment.TrackCode,
		shipment.OrderIds,
		shipment.CustomerName,
		shipment.CustomerPhone,
		shipment.CustomerEmail,
		shipment.ReceiverName,
		shipment.ReceiverPhone,
		shipment.DeliveryCountry,
		shipment.DeliveryCity,
		shipment.DeliveryAddress,
		shipment.ShippingCompany,
		shipment.Comments,
	};

	private readonly System.Text.StringBuilder sbPlain = new();
	private readonly System.Text.StringBuilder sbHtml = new();
	private readonly Ganss.Xss.HtmlSanitizer sanitizer = new();

	public string BuildPlainText() => sbPlain.ToString();
	public string BuildHTML() => sbHtml.ToString();

	public EmailTextBuilder AppendLine(string line)
	{
		sbPlain.AppendLine(line);
		sbHtml.
			Append("<div>").
			Append(sanitizer.Sanitize(line)).
			Append("</div>");
		return this;
	}

	public EmailTextBuilder AppendTableStart()
	{
		sbHtml.Append("<table>");
		return this;
	}

	public EmailTextBuilder AppendTableEnd()
	{
		sbHtml.Append("</table>");
		return this;
	}

	public EmailTextBuilder AppendShipmentTableHeader()
	{
		sbHtml.Append("<tr>");
		foreach (string columnName in columnNames)
		{
			sbPlain.
				Append(columnName).
				Append('\n');
			sbHtml.
				Append("<td><b>").
				Append(columnName).
				Append("</b></td>");
		}
		sbPlain.Append("\n---- ---- ----\n");
		sbHtml.Append("</tr>");
		return this;
	}

	public EmailTextBuilder AppendShipmentRecord(Shop.Storing.Models.Shipment shipment)
	{
		//todo: placeholder, replace with proper format
		sbHtml.Append("<tr>");
		foreach (string property in properties)
		{
			sbPlain.
				Append(property).
				Append('\t');
			sbHtml.
				Append("<td>").
				Append(sanitizer.Sanitize(property)).
				Append("</td>");
		}
		sbPlain.Append('\n');
		sbHtml.Append("</tr>");
		return this;
	}
}
