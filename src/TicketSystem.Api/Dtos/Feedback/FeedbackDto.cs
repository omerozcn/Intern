namespace TicketSystem.Dtos.Feedback;

public sealed class FeedbackDto
{
    public int Id { get; set; }
    public string FeedbackContent { get; set; } = string.Empty;
}
