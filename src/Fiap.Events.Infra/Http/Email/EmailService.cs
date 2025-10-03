using Microsoft.Extensions.Logging;

namespace Fiap.Events.Infra.Http.Email
{
    public class EmailService(
        IOptionsSnapshot<AppSettings> appSettings,
        ILogger<EmailService> logger
            ) : IEmailService
    {
        private readonly AppSettings _appSettings = appSettings.Value;
        public async System.Threading.Tasks.Task<bool> SendEmail(string name, string email)
        {
            logger.LogInformation("Starting email send process for {Email} ({Name})", email, name);
            
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
                logger.LogInformation("Sending email to {Email} with subject: {Subject}", email, _appSettings.Subject);
                CreateSmtpEmail result = apiInstance.SendTransacEmail(emailContent);
                logger.LogInformation("Email sent successfully. Message ID: {MessageId}", result.MessageId);

                return !string.IsNullOrEmpty(result.MessageId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send email to {Email} ({Name})", email, name);
                return false;
            }
        }
    }
}
