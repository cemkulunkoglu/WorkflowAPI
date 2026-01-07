namespace MessagesService.Events;

public class EventEnvelope<T>
{
    public string EventName { get; set; } = "";
    public T Payload { get; set; } = default!;
}
