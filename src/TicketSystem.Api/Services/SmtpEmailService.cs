using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TicketSystem.Configuration;
using TicketSystem.Interfaces;

namespace TicketSystem.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;

    public SmtpEmailService(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(new MailboxAddress(recipientName, recipientEmail));
        message.Subject = "Ticket System password reset";

        var encodedName = WebUtility.HtmlEncode(recipientName);
        var encodedUrl = WebUtility.HtmlEncode(resetUrl);
        message.Body = new BodyBuilder
        {
            TextBody = $"Hello {recipientName},\n\nReset your password using this link: {resetUrl}\n\nIf you did not request this, you can ignore this email.",
            HtmlBody = $"<p>Hello {encodedName},</p><p><a href=\"{encodedUrl}\">Reset your password</a>. This link expires in two hours.</p><p>If you did not request this, you can ignore this email.</p>"
        }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = Enum.Parse<SecureSocketOptions>(_options.SocketOptions, ignoreCase: true);
        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.FromEmail)
            || _options.Port is < 1 or > 65535
            || !Enum.TryParse<SecureSocketOptions>(_options.SocketOptions, ignoreCase: true, out _)
            || (string.IsNullOrWhiteSpace(_options.UserName) != string.IsNullOrWhiteSpace(_options.Password)))
        {
            throw new InvalidOperationException(
                "SMTP is not configured. Set Smtp__Host, Smtp__FromEmail and, when required, Smtp credentials.");
        }
    }
}
