using System;
using System.Threading.Tasks;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Mailjet.Client.TransactionalEmails;
using Mailjet.Client.TransactionalEmails.Response;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ShopServices.Mailing;

/// <summary>
/// Proxy for Mailjet, hopefully will be easy-ish to switch to a different provider if needed
/// </summary>
internal class EmailSender
{
	private readonly Mailjet.Client.MailjetClient client;
	private readonly ILogger logger;
	private static readonly string addressToSendFrom = Environment.GetEnvironmentVariable("SHOPSERVICES_MAILFROM");
	private static readonly string zipPassword = Environment.GetEnvironmentVariable("SHOPSERVICES_MAILZIPPASSWORD");
	private static string apiKey = Environment.GetEnvironmentVariable("MAILJET_API_KEY");
	private static string apiSecret = Environment.GetEnvironmentVariable("MAILJET_API_SECRET");

	public EmailSender(ILogger logger)
	{
		this.logger = logger;
		if (apiKey is null or "" || apiSecret is null or "" || zipPassword is null or "")
		{
			logger.LogError("MAILJET_API_KEY, MAILJET_API_SECRET and SHOPSERVICES_MAILZIPPASSWORD envars " +
				"must be set for emails to work");
			logger.LogError("Sending mails will fail");
			return;
		}
		client = new(apiKey, apiSecret);
	}

	public async Task SendSingleAsync(
		string to,
		string subject,
		string plainTextContent,
		string htmlContent,
		bool sendContentAsAttachment = false)
	{
		if (client is null)
		{
			logger.LogError(
				$"Can not send email to {to} on {subject}: api key and/or secret were not initialized properly");
			return;
		}
		TransactionalEmailBuilder emailBuilder = new TransactionalEmailBuilder().
			WithFrom(new SendContact(addressToSendFrom)).
			WithSubject(subject).
			WithTo(new SendContact(to));
		if (sendContentAsAttachment)
		{
			try
			{
				string attachmentName = $"{subject}.zip";
				using Ionic.Zip.ZipFile zipfile = new(attachmentName, System.Text.Encoding.UTF8);
				zipfile.Encryption = Ionic.Zip.EncryptionAlgorithm.WinZipAes256;
				zipfile.Password = zipPassword;
				zipfile.AddEntry("content.txt", plainTextContent);
				System.IO.MemoryStream memstream = new();
				zipfile.Save(memstream);
				memstream.Position = 0;
				emailBuilder.
					WithTextPart($"Contents of this message were moved to the attachment").
					WithAttachment(new Attachment(
						attachmentName,
						"application/zip",
						System.Convert.ToBase64String(memstream.GetBuffer(), 0, (int)memstream.Length)));
			}
			catch (Exception ex)
			{
				logger.LogError($"Could not compress contents into attachment due to error:\n{ex}");
				throw;
			}
		}
		else
		{
			emailBuilder.
				WithHtmlPart(htmlContent).
				WithTextPart(plainTextContent);
		}
		TransactionalEmail email = emailBuilder.Build();
		// invoke API to send email
		TransactionalEmailResponse response = await client.SendTransactionalEmailAsync(email);
	}
}
