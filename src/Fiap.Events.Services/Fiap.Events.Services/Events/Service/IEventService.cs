namespace Fiap.Events.Services.Events.Service
{
    public interface IEventService
    {
        Task<bool> SendCreateUserEmail(CreateAccount messageDeserialized);
    }
}
