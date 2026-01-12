namespace AuthServerAPI.Interfaces
{
    public interface IEventPublisher
    {
        void Publish<T>(string eventName, T payload);
    }
}
