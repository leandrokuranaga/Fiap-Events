namespace Fiap.Events.Infra.Http.Email
{
    public class EmailService(
        IOptionsSnapshot<AppSettings> appSettings
            ) : IEmailService
    {
        private readonly AppSettings _appSettings = appSettings.Value;
        public async System.Threading.Tasks.Task<bool> SendEmail(string name, string email)
        {
            brevo_csharp.Client.Configuration.Default.ApiKey["api-key"] = _appSettings.BrevoAPIKey;

            var toEmail = new SendSmtpEmailTo(email, name);
            var toEmails = new List<SendSmtpEmailTo>();

            toEmails.Add(toEmail);

            var sender = new SendSmtpEmailSender
            {
                Email = _appSettings.EmailSender,
                Name = _appSettings.NameSender
            };

            var apiInstance = new TransactionalEmailsApi();

            var emailContent = new SendSmtpEmail
            {
                Sender = sender,
                To = toEmails,
                Subject = _appSettings.Subject,
                HtmlContent = $"<html><body><h1>Olá {name}!</h1><p>Obrigado por se inscrever no FCG.</p></body></html>"
            };

            try
            {
                CreateSmtpEmail result = apiInstance.SendTransacEmail(emailContent);
                return !string.IsNullOrEmpty(result.MessageId);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling SendTransacEmail " + e.Message);
                return false;
            }
        }
    }
}
