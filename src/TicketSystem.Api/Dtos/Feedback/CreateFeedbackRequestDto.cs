using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Dtos.Feedback;

public sealed class CreateFeedbackRequestDto
{
    private string _feedbackContent = string.Empty;

    /// <summary>
    /// Trimmed on assignment so validation runs against the value that is actually stored.
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string FeedbackContent
    {
        get => _feedbackContent;
        set => _feedbackContent = value?.Trim() ?? string.Empty;
    }
}
