using Fiap.Contracts.Events;
using Newtonsoft.Json.Linq;

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
    /// Lambda function triggered by SQS messages.  
    /// It normalizes the incoming payload, deserializes the event, maps it into a CreateAccount object
    /// and calls the event service to send an email notification.
    /// </summary>
    public async Task CreateUserFunction(SQSEvent sqsEvent, ILambdaContext context)
    {
        if (sqsEvent?.Records == null || sqsEvent.Records.Count == 0)
        {
            _log.LogError("SQS event or records is null/empty.");
            return;
        }

        _log.LogInformation("Processing {Count} messages from SQS queue.", sqsEvent.Records.Count);

        foreach (var record in sqsEvent.Records)
        {
            await ProcessMessageAsync(record, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        try
        {
            _log.LogInformation("Processing SQS message {MessageId}", message.MessageId);
            _log.LogDebug("Raw message body: {Body}", message.Body);

            var finalJson = ExtractEventJson(message.Body);

            _log.LogDebug("Normalized JSON payload: {Payload}", finalJson);

            var evt = JsonConvert.DeserializeObject<CreateUserIntegrationEvent>(finalJson);
            if (evt is null)
            {
                _log.LogError("Failed to deserialize payload into CreateUserIntegrationEvent.");
                return;
            }

            var account = new CreateAccount
            {
                Id = evt.UserId,
                Name = evt.Name,
                Email = new Email { Address = evt.Email },
                TypeUser = evt.Type,
                Active = evt.Active
            };

            _log.LogInformation("Mapped event to CreateAccount: Name={Name}, Email={Email}",
                                account.Name, account.Email?.Address);

            await _eventService.SendCreateUserEmail(account);

            _log.LogInformation("User email sent successfully to: {Email}", account.Email?.Address);
        }
        catch (JsonException jsonEx)
        {
            _log.LogError(jsonEx,
                "JSON deserialization error while processing message {MessageId}. Payload: {Body}",
                message.MessageId, message.Body);
            throw;
        }
        catch (FormatException formatEx)
        {
            _log.LogError(formatEx,
                "Base64 decoding error while processing message {MessageId}. Payload: {Body}",
                message.MessageId, message.Body);
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unexpected error while processing message {MessageId}", message.MessageId);
            throw;
        }
        finally
        {
            _log.LogInformation("Finished processing SQS message {MessageId}.", message.MessageId);
        }
    }

    /// <summary>
    /// Extracts the inner JSON event from the raw SQS message body.  
    /// Supports three formats:
    ///  1) Rebus envelope: { "headers": {...}, "body": "base64(...json...)" }
    ///  2) Base64 encoded JSON directly in the body
    ///  3) Plain JSON already in the body
    /// </summary>
    private static string ExtractEventJson(string rawBody)
    {
        try
        {
            var token = JToken.Parse(rawBody);

            if (token.Type == JTokenType.Object && token["body"] is JToken bodyToken)
            {
                var bodyString = bodyToken.Type == JTokenType.String
                    ? bodyToken.Value<string>()!
                    : bodyToken.ToString(Newtonsoft.Json.Formatting.None);

                if (TryBase64Decode(bodyString, out var decoded))
                {
                    if (decoded.StartsWith("\"") && decoded.EndsWith("\""))
                        return JsonConvert.DeserializeObject<string>(decoded)!;

                    return decoded;
                }

                return bodyString;
            }
        }
        catch (JsonReaderException jre)
        {
            Console.WriteLine($"ExtractEventJson: raw body is not an object JSON. Error={jre.Message}");            
        }

        if (TryBase64Decode(rawBody, out var decodedBody))
        {
            if (decodedBody.StartsWith("\"") && decodedBody.EndsWith("\""))
                return JsonConvert.DeserializeObject<string>(decodedBody)!;

            return decodedBody;
        }

        return rawBody;
    }

    /// <summary>
    /// Attempts to decode a base64 string into UTF-8 text.  
    /// Returns true if decoding succeeded, false otherwise.
    /// </summary>
    private static bool TryBase64Decode(string input, out string decoded)
    {
        try
        {
            var bytes = Convert.FromBase64String(input);
            decoded = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch (FormatException)
        {
            decoded = string.Empty;
            return false;
        }
    }
}
