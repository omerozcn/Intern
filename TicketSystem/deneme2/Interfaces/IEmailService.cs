namespace TicketSystem.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken = default);
}
