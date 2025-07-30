namespace Fiap.Events.Unit.Tests._2._Application_Layer_Tests
{
    public class EventServiceTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<EventService>> _mockLogger;
        private readonly EventService _eventService;

        public EventServiceTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<EventService>>();
            _eventService = new EventService(_mockEmailService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SendCreateUserEmail_WithValidAccount_ShouldReturnTrue_WhenEmailSentSuccessfully()
        {
            // Arrange
            var createAccount = new CreateAccount
            {
                Name = "John Doe",
                Email = new Email { Address = "john.doe@example.com" },
                TypeUser = 1,
                Active = true,
                Id = 123
            };

            _mockEmailService
                .Setup(x => x.SendEmail(createAccount.Name, createAccount.Email.Address))
                .ReturnsAsync(true);

            // Act
            var result = await _eventService.SendCreateUserEmail(createAccount);

            // Assert
            Assert.True(result);
            _mockEmailService.Verify(x => x.SendEmail(createAccount.Name, createAccount.Email.Address), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Email sent successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendCreateUserEmail_WithValidAccount_ShouldReturnFalse_WhenEmailFailsToSend()
        {
            // Arrange
            var createAccount = new CreateAccount
            {
                Name = "Jane Smith",
                Email = new Email { Address = "jane.smith@example.com" },
                TypeUser = 2,
                Active = true,
                Id = 456
            };

            _mockEmailService
                .Setup(x => x.SendEmail(createAccount.Name, createAccount.Email.Address))
                .ReturnsAsync(false);

            // Act
            var result = await _eventService.SendCreateUserEmail(createAccount);

            // Assert
            Assert.False(result);
            _mockEmailService.Verify(x => x.SendEmail(createAccount.Name, createAccount.Email.Address), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendCreateUserEmail_WhenEmailServiceThrowsException_ShouldReturnFalse()
        {
            // Arrange
            var createAccount = new CreateAccount
            {
                Name = "Error User",
                Email = new Email { Address = "error@example.com" },
                TypeUser = 1,
                Active = true,
                Id = 789
            };

            var expectedException = new InvalidOperationException("Email service error");
            
            _mockEmailService
                .Setup(x => x.SendEmail(createAccount.Name, createAccount.Email.Address))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _eventService.SendCreateUserEmail(createAccount);

            // Assert
            Assert.False(result);
            _mockEmailService.Verify(x => x.SendEmail(createAccount.Name, createAccount.Email.Address), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while sending the email")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SendCreateUserEmail_WithDifferentUserTypes_ShouldCallEmailServiceCorrectly()
        {
            // Arrange
            var createAccounts = new[]
            {
                new CreateAccount
                {
                    Name = "Admin User",
                    Email = new Email { Address = "admin@example.com" },
                    TypeUser = 1,
                    Active = true,
                    Id = 1
                },
                new CreateAccount
                {
                    Name = "Regular User",
                    Email = new Email { Address = "user@example.com" },
                    TypeUser = 2,
                    Active = true,
                    Id = 2
                }
            };

            _mockEmailService
                .Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act & Assert
            foreach (var account in createAccounts)
            {
                var result = await _eventService.SendCreateUserEmail(account);
                Assert.True(result);
            }

            _mockEmailService.Verify(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SendCreateUserEmail_WithInactiveUser_ShouldStillSendEmail()
        {
            // Arrange
            var createAccount = new CreateAccount
            {
                Name = "Inactive User",
                Email = new Email { Address = "inactive@example.com" },
                TypeUser = 1,
                Active = false,
                Id = 999
            };

            _mockEmailService
                .Setup(x => x.SendEmail(createAccount.Name, createAccount.Email.Address))
                .ReturnsAsync(true);

            // Act
            var result = await _eventService.SendCreateUserEmail(createAccount);

            // Assert
            Assert.True(result);
            _mockEmailService.Verify(x => x.SendEmail(createAccount.Name, createAccount.Email.Address), Times.Once);
        }
    }
}
