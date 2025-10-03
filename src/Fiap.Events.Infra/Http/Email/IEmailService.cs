namespace Fiap.Events.Infra.Http.Email
{
    public interface IEmailService
    {
        Task<bool> SendEmail(string name, string email);
    }
}
