namespace WorkflowManagemetAPI.Interfaces.Messaging
{
    public interface IEventPublisher
    {
        void Publish(string eventName, object payload);
    }
}
