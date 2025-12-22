public class ChatMessageEvent
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");
    public string Sender { get; set; } = "User";
    public string Text { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}