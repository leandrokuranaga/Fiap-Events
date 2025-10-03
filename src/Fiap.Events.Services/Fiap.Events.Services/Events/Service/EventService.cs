namespace Fiap.Events.Services.Events.Service
{
    public class EventService(
        IEmailService emailService,
        ILogger<EventService> logger
        ) : IEventService
    {
        public async Task<bool> SendCreateUserEmail(CreateAccount messageDeserialized)
        {
            try
            {
                var success = await emailService.SendEmail(messageDeserialized.Name, messageDeserialized.Email.Address);

                if (success)
                {
                    logger.LogInformation("Email sent successfully to {Email}",
                        messageDeserialized.Email.Address);
                }
                else
                {
                    logger.LogWarning("Failed to send email to {Email}",
                        messageDeserialized.Email.Address);
                }

                return success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while sending the email.");
                return false;
            }
        }
    }
}
