using System.Threading.Channels;
using TicketSystem.Interfaces;

namespace TicketSystem.Infrastructure;

/// <summary>A password reset message waiting to be delivered.</summary>
public sealed record PasswordResetEmail(string ToEmail, string ToName, string ResetUrl);

/// <summary>
/// Hands password reset mail to a background worker.
///
/// Sending inline made <c>forgot-password</c> leak account existence: a known address
/// paid for a full SMTP round trip before the response was written, an unknown one
/// returned immediately. Queueing makes both branches indistinguishable.
/// </summary>
public interface IPasswordResetNotifier
{
    /// <summary>Queues a message. Never blocks and never throws.</summary>
    void Enqueue(PasswordResetEmail message);
}

internal sealed class PasswordResetNotifier : IPasswordResetNotifier
{
    // Bounded so a burst cannot grow unboundedly; the oldest pending message is
    // dropped rather than slowing the request path down.
    private readonly Channel<PasswordResetEmail> _channel =
        Channel.CreateBounded<PasswordResetEmail>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public void Enqueue(PasswordResetEmail message) => _channel.Writer.TryWrite(message);

    public IAsyncEnumerable<PasswordResetEmail> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

internal sealed class PasswordResetEmailDispatcher : BackgroundService
{
    private readonly PasswordResetNotifier _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PasswordResetEmailDispatcher> _logger;

    public PasswordResetEmailDispatcher(
        IPasswordResetNotifier queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PasswordResetEmailDispatcher> logger)
    {
        _queue = (PasswordResetNotifier)queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendPasswordResetAsync(
                    message.ToEmail,
                    message.ToName,
                    message.ResetUrl,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // The address is deliberately not logged, and one failed delivery must
                // not take the worker down.
                _logger.LogError(exception, "Password reset email delivery failed.");
            }
        }
    }
}
