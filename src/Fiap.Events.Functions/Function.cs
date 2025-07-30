[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Fiap.Events.Functions;

public class Function
{
    private readonly IEventService _eventService;
    private readonly ILogger<Function> _log;

    public Function()
    {
        _eventService = Program.ServiceProvider.GetRequiredService<IEventService>();
        _log = Program.ServiceProvider.GetRequiredService<ILogger<Function>>();
    }

    public Function(IEventService eventService, ILogger<Function> log)
    {
        _eventService = eventService;
        _log = log;
    }

    /// <summary>
    /// A function that processes SQS messages from fcg-queue and sends email to user
    /// </summary>
    /// <param name="sqsEvent">The SQS event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task CreateUserFunction(SQSEvent sqsEvent, ILambdaContext context)
    {
        if (sqsEvent?.Records == null)
        {
            _log.LogError("SQS event or records is null");
            return;
        }

        _log.LogInformation($"Processing {sqsEvent.Records.Count} messages from SQS queue");

        foreach (var record in sqsEvent.Records)
        {
            await ProcessMessageAsync(record, context);
        }
    }

    private async System.Threading.Tasks.Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        try
        {
            _log.LogInformation($"Processing SQS message with ID: {message.MessageId}");
            _log.LogInformation($"Raw message body: {message.Body}");

            string finalJsonPayload;

            try
            {
                var messageWrapper = JsonConvert.DeserializeObject<MessageWrapper>(message.Body);
                
                if (messageWrapper?.Body != null)
                {
                    _log.LogInformation("Message has wrapper format with body property");
                    
                    var base64Bytes = Convert.FromBase64String(messageWrapper.Body);
                    var decodedString = Encoding.UTF8.GetString(base64Bytes);
                    _log.LogInformation($"Decoded Base64: {decodedString}");

                    if (decodedString.StartsWith("\"") && decodedString.EndsWith("\""))
                    {
                        finalJsonPayload = JsonConvert.DeserializeObject<string>(decodedString);
                        _log.LogInformation($"Unescaped JSON string: {finalJsonPayload}");
                    }
                    else
                    {
                        finalJsonPayload = decodedString;
                    }
                }
                else
                {
                    var base64Bytes = Convert.FromBase64String(message.Body);
                    finalJsonPayload = Encoding.UTF8.GetString(base64Bytes);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning($"Failed to decode message: {ex.Message}");
                finalJsonPayload = message.Body;
            }

            _log.LogInformation($"Final JSON to deserialize: {finalJsonPayload}");
            var messageDeserialized = JsonConvert.DeserializeObject<CreateAccount>(finalJsonPayload);
            
            _log.LogInformation($"Message deserialized successfully - Name: {messageDeserialized?.Name}, Email: {messageDeserialized?.Email?.Address}");

            if (messageDeserialized != null)
            {
                await _eventService.SendCreateUserEmail(messageDeserialized);
                _log.LogInformation($"User email sent successfully to: {messageDeserialized.Email.Address}");
            }
            else
            {
                _log.LogError("Failed to deserialize message to CreateAccount");
            }
        }
        catch (Exception ex)
        {
            _log.LogError($"An error occurred while processing the user email: {ex.Message}");
            _log.LogError($"Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            _log.LogInformation($"SQS message {message.MessageId} processing completed.");
        }
    }
}