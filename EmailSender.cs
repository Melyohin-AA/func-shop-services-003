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

	public EmailSender(ILogger logger)
	{
		string apiKey = Environment.GetEnvironmentVariable("MAILJET_API_KEY");
		string apiSecret = Environment.GetEnvironmentVariable("MAILJET_API_SECRET");
		addressToSendFrom = Environment.GetEnvironmentVariable("SHOPSERVICES_MAILFROM");

		if (apiKey is null or "" || apiSecret is null or "")
		{
			throw new ArgumentException("MAILJET_API_KEY and MAILJET_API_SECRET envars must be set for email notifications to work");
		}
		client = new(apiKey, apiSecret);
		this.logger = logger;
	}
	public async Task SendSingleAsync(
		string to,
		string subject,
		string plainTextContent,
		string htmlContent)
	{
		MailjetRequest request = new MailjetRequest
		{
			Resource = Send.Resource
		};

		// construct your email with builder
		var email = new TransactionalEmailBuilder()
			   .WithFrom(new SendContact(addressToSendFrom))
			   .WithSubject(subject)
			   .WithHtmlPart(htmlContent)
			   .WithTextPart(plainTextContent)
			   .WithTo(new SendContact(to))
			   .Build();
		// invoke API to send email
		TransactionalEmailResponse response = await client.SendTransactionalEmailAsync(email);
	}
}

