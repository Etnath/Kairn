using Kairn.Application.Features.AR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Kairn.Infrastructure.Email;

public class SmtpEmailService(
    IOptions<EmailOptions> opts,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _opts = opts.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_opts.SmtpHost)
                             && !string.IsNullOrWhiteSpace(_opts.FromAddress);

    public async Task SendAsync(
        string to, string subject, string htmlBody,
        byte[]? attachment = null, string? attachmentName = null,
        CancellationToken ct = default)
    {
        var from = new MailAddress(_opts.FromAddress, _opts.FromName);
        using var msg = new MailMessage(from, new MailAddress(to))
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };

        MemoryStream? ms = null;
        if (attachment is not null)
        {
            ms = new MemoryStream(attachment);
            msg.Attachments.Add(new Attachment(ms, attachmentName ?? "invoice.pdf", "application/pdf"));
        }

        try
        {
            using var client = new SmtpClient(_opts.SmtpHost, _opts.SmtpPort)
            {
                EnableSsl = _opts.SmtpUseSsl,
                Credentials = new NetworkCredential(_opts.SmtpUsername, _opts.SmtpPassword),
            };
            await client.SendMailAsync(msg, ct);
            logger.LogInformation("Reminder email sent to {To}", to);
        }
        finally
        {
            ms?.Dispose();
        }
    }
}
