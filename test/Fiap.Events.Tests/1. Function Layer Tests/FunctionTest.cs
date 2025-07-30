namespace Fiap.Events.Unit.Tests._1._Function_Layer_Tests;

public class FunctionTest
{
    private readonly Mock<IEventService> _mockEventService;
    private readonly Mock<ILogger<Function>> _mockLogger;
    private readonly Function _function;

    public FunctionTest()
    {
        _mockEventService = new Mock<IEventService>();
        _mockLogger = new Mock<ILogger<Function>>();
        _function = new Function(_mockEventService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateUserFunction_WithMessageWrapperFormat_ShouldProcessSuccessfully()
    {
        // Arrange
        var createAccount = new CreateAccount
        {
            Name = "Test User",
            Email = new Email { Address = "test@example.com" }
        };

        var jsonPayload = JsonConvert.SerializeObject(createAccount);
        var base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonPayload));
        var messageWrapper = new MessageWrapper
        {
            Body = base64Payload
        };
        var messageBody = JsonConvert.SerializeObject(messageWrapper);

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                new() {
                    MessageId = "test-message-id",
                    Body = messageBody
                }
            ]
        };

        var context = new TestLambdaContext();

        _mockEventService
            .Setup(x => x.SendCreateUserEmail(It.IsAny<CreateAccount>()))
            .ReturnsAsync(true);

        // Act
        await _function.CreateUserFunction(sqsEvent, context);

        // Assert
        _mockEventService.Verify(x => x.SendCreateUserEmail(It.Is<CreateAccount>(
            ca => ca.Name == createAccount.Name && ca.Email.Address == createAccount.Email.Address)), 
            Times.Once);
    }

    [Fact]
    public async Task CreateUserFunction_WithPlainTextFallback_ShouldProcessSuccessfully()
    {
        // Arrange
        var createAccount = new CreateAccount
        {
            Name = "Test User Plain",
            Email = new Email { Address = "testplain@example.com" }
        };

        var messageBody = JsonConvert.SerializeObject(createAccount);

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                new() {
                    MessageId = "test-message-id-3",
                    Body = messageBody
                }
            ]
        };

        var context = new TestLambdaContext();

        _mockEventService
            .Setup(x => x.SendCreateUserEmail(It.IsAny<CreateAccount>()))
            .ReturnsAsync(true);

        // Act
        await _function.CreateUserFunction(sqsEvent, context);

        // Assert
        _mockEventService.Verify(x => x.SendCreateUserEmail(It.Is<CreateAccount>(
            ca => ca.Name == createAccount.Name && ca.Email.Address == createAccount.Email.Address)), 
            Times.Once);
    }

    [Fact]
    public async Task CreateUserFunction_WithEscapedJsonString_ShouldProcessSuccessfully()
    {
        // Arrange
        var createAccount = new CreateAccount
        {
            Name = "Test User Escaped",
            Email = new Email { Address = "testescaped@example.com" }
        };

        var jsonPayload = JsonConvert.SerializeObject(createAccount);
        var escapedJson = JsonConvert.SerializeObject(jsonPayload);
        var base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(escapedJson));
        var messageWrapper = new MessageWrapper
        {
            Body = base64Payload
        };
        var messageBody = JsonConvert.SerializeObject(messageWrapper);

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                new() {
                    MessageId = "test-message-id-4",
                    Body = messageBody
                }
            ]
        };

        var context = new TestLambdaContext();

        _mockEventService
            .Setup(x => x.SendCreateUserEmail(It.IsAny<CreateAccount>()))
            .ReturnsAsync(true);

        // Act
        await _function.CreateUserFunction(sqsEvent, context);

        // Assert
        _mockEventService.Verify(x => x.SendCreateUserEmail(It.Is<CreateAccount>(
            ca => ca.Name == createAccount.Name && ca.Email.Address == createAccount.Email.Address)), 
            Times.Once);
    }

    [Fact]
    public async Task CreateUserFunction_WithNullSQSEvent_ShouldNotProcessAnyMessages()
    {
        // Arrange
        var context = new TestLambdaContext();

        // Act
        await _function.CreateUserFunction(null, context);

        // Assert
        _mockEventService.Verify(x => x.SendCreateUserEmail(It.IsAny<CreateAccount>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserFunction_WithEmptyRecords_ShouldNotProcessAnyMessages()
    {
        // Arrange
        var sqsEvent = new SQSEvent { Records = new List<SQSEvent.SQSMessage>() };
        var context = new TestLambdaContext();

        // Act
        await _function.CreateUserFunction(sqsEvent, context);

        // Assert
        _mockEventService.Verify(x => x.SendCreateUserEmail(It.IsAny<CreateAccount>()), Times.Never);
    }
}
