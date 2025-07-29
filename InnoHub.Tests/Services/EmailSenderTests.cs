using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Tests.Services
{
    public class EmailSenderTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly InnoHub.Service.EmailSenderService.EmailSender _emailSender;

        public EmailSenderTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(x => x["EmailSender:Email"]).Returns("test@example.com");
            _mockConfiguration.Setup(x => x["EmailSender:AppPassword"]).Returns("testpassword");

            _emailSender = new InnoHub.Service.EmailSenderService.EmailSender(_mockConfiguration.Object);
        }

        [Fact]
        public void Constructor_WithMissingEmailConfig_ShouldThrowArgumentNullException()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(x => x["EmailSender:Email"]).Returns((string)null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new InnoHub.Service.EmailSenderService.EmailSender(config.Object));
        }

        [Fact]
        public void Constructor_WithMissingPasswordConfig_ShouldThrowArgumentNullException()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(x => x["EmailSender:Email"]).Returns("test@example.com");
            config.Setup(x => x["EmailSender:AppPassword"]).Returns((string)null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new InnoHub.Service.EmailSenderService.EmailSender(config.Object));
        }

        [Fact]
        public async Task SendEmailAsync_WithNullEmail_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _emailSender.SendEmailAsync(null, "Subject", "Message"));
        }

        [Fact]
        public async Task SendEmailAsync_WithEmptyEmail_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _emailSender.SendEmailAsync("", "Subject", "Message"));
        }

        [Fact]
        public async Task SendEmailAsync_WithNullSubject_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _emailSender.SendEmailAsync("test@example.com", null, "Message"));
        }

        [Fact]
        public async Task SendEmailAsync_WithNullMessage_ShouldThrowArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _emailSender.SendEmailAsync("test@example.com", "Subject", null));
        }
    }
}
