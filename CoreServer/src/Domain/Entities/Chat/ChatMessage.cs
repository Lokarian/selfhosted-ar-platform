namespace CoreServer.Domain.Entities.Chat;

public class ChatMessage : BaseEntity
{
    public Guid SessionId { get; set; }
    public ChatSession Session { get; set; } = null!;

    public Guid SenderId { get; set; }
    public AppUser Sender { get; set; } = null!;
    public string Text { get; set; } = null!;
    public DateTime SentAt { get; set; }


    public ChatMessage()
    {
        SentAt = DateTime.UtcNow;
    }
}