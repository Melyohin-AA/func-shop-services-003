using HtmlAgilityPack;

namespace ShopServices;

/// <summary>
/// Constructs both HTML and plaintext message bodies for a notification email
/// </summary>
internal class EmailTextBuilder
{
	private readonly System.Text.StringBuilder sbPlain = new();

	private readonly System.Text.StringBuilder sbHtml = new();
	Ganss.Xss.HtmlSanitizer sanitizer = new();

	public string BuildPlainText() => sbPlain.ToString();
	public string BuildHTML() => sbHtml.ToString();
	public EmailTextBuilder AppendLine(string line)
	{

		sbPlain.AppendLine(line);
		string
			divo = "<div>",
			divc = "</div>";
		sbHtml
			.Append(divo)
			.Append(sanitizer.Sanitize(line))
			.Append(divc);
		return this;
	}
	public EmailTextBuilder AppendTableStart()
	{
		string tableo = "<table>";
		sbHtml.Append(tableo);
		return this;
	}
	public EmailTextBuilder AppendTableEnd()
	{
		string tablec = "</table>";
		sbHtml.Append(tablec);
		return this;
	}
	public EmailTextBuilder AppendShipmentTableHeader()
	{
		string[] columnNames =
		{
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
		char tab = '\t';
		char newline = '\n';
		string
			tro = "<tr>",
			trc = "</tr>",
			tdo = "<td>",
			tdc = "</td>",
			bo = "<b>",
			bc = "</b>";
		sbHtml.Append(tro);
		foreach (string columnName in columnNames)
		{
			sbPlain
				.Append(columnName)
				.Append(tab);
			sbHtml
				.Append(tdo)
				.Append(bo)
				.Append(columnName)
				.Append(bc)
				.Append(tdc);
		}
		sbPlain
			.Append(newline)
			.Append("---- ---- ----")
			.Append(newline);
		sbHtml.Append(trc);
		return this;

	}
	public EmailTextBuilder AppendShipmentRecord(Shop.Storing.Models.Shipment shipment)
	{
		//todo: placeholder, replace with proper format
		char tab = '\t';
		char newline = '\n';
		string[] properties = {

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

		string
			tro = "<tr>",
			trc = "</tr>",
			tdo = "<td>",
			tdc = "</td>";
		sbHtml.Append(tro);
		foreach (string property in properties)
		{
			sbPlain
				.Append(property)
				.Append(tab);
			sbHtml
				.Append(tdo)
				.Append(sanitizer.Sanitize(property))
				.Append(tdc);
		}
		sbPlain.Append(newline);
		sbHtml.Append(trc);
		return this;
	}
}