using System;
using System.Threading.Tasks;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Mailjet.Client.TransactionalEmails;
using Mailjet.Client.TransactionalEmails.Response;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ShopServices;

/// <summary>
/// Proxy for Mailjet, hopefully will be easy-ish to switch to a different provider if needed
/// </summary>
internal class EmailSender
{
	private readonly Mailjet.Client.MailjetClient client;
	private readonly ILogger logger;
	private readonly string addressToSendFrom;
	private readonly string zipPassword;

	public EmailSender(ILogger logger)
	{
		string apiKey = Environment.GetEnvironmentVariable("MAILJET_API_KEY");
		string apiSecret = Environment.GetEnvironmentVariable("MAILJET_API_SECRET");
		this.logger = logger;
		addressToSendFrom = Environment.GetEnvironmentVariable("SHOPSERVICES_MAILFROM");
		zipPassword = Environment.GetEnvironmentVariable("SHOPSERVICES_MAILZIPPASSWORD") ?? "ss03";

		if (apiKey is null or "" || apiSecret is null or "")
		{
			logger.LogError("MAILJET_API_KEY and MAILJET_API_SECRET envars must be set for email notifications to work");
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
			logger.LogError($"Can not send email to {to} on {subject}: api key and/or secret were not initialized properly");
			return;
		}
		TransactionalEmailBuilder emailBuilder = new TransactionalEmailBuilder()
			.WithFrom(new SendContact(addressToSendFrom))
			.WithSubject(subject)
			.WithTo(new SendContact(to));
		if (sendContentAsAttachment)
		{
			try
			{
				const string attachmentName = "content.zip";
				using Ionic.Zip.ZipFile zipfile = new(attachmentName, System.Text.Encoding.UTF8);
				zipfile.Encryption = Ionic.Zip.EncryptionAlgorithm.WinZipAes256;
				zipfile.Password = zipPassword;
				zipfile.AddEntry("content.txt", plainTextContent);
				
				System.IO.MemoryStream memstream = new();
				zipfile.Save(memstream);
				memstream.Position = 0;
				byte[] buf = new byte[memstream.Length];
				await memstream.ReadAsync(buf, 0, (int)memstream.Length);
				emailBuilder
					.WithTextPart($"Contents of this message were moved to the attachment")
					.WithAttachment(new Attachment(attachmentName, "application/zip", System.Convert.ToBase64String(buf)));
			}
			catch (Exception ex)
			{
				logger.LogError($"Could not compress contents into attachment due to error:\n{ex}");
				throw;
			}

		}
		else
		{
			emailBuilder
			.WithHtmlPart(htmlContent)
			.WithTextPart(plainTextContent);
		}
		TransactionalEmail email = emailBuilder.Build();
		// invoke API to send email
		TransactionalEmailResponse response = await client.SendTransactionalEmailAsync(email);
	}
}

