using AutoMapper;
using Ecommerce_platforms.Repository.Auth;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using InnoHub.Tests.BaseTests;
using InnoHub.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using FluentAssertions;
using InnoHub.Controllers;
using InnoHub.Repository.Repository;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InnoHub.Tests.Controllers
{
    public class NotificationControllerTests : BaseControllerTest
    {
        private readonly NotificationController _controller;
        private readonly Mock<IAuth> _mockAuth;

        public NotificationControllerTests()
        {
            // 1. Create the mock
            _mockAuth = new Mock<IAuth>();

            // 2. Set up UnitOfWork to return the mocked IAuth
            MockUnitOfWork.Setup(u => u.Auth).Returns(_mockAuth.Object);

            // 3. Pass the mock itself to SetupAuthMocks
            MockHelper.SetupAuthMocks(_mockAuth);

            // 4. Initialize the controller
            _controller = new NotificationController(
                MockUnitOfWork.Object,
                GetMockLogger<NotificationController>().Object
            );
        }


        [Fact]
        public async Task GetNotificationHistory_WithValidUser_ShouldReturnHistory()
        {
            // Arrange
            var messages = new List<DealMessage>
        {
            new DealMessage
            {
                Id = 1,
                RecipientId = "test-user-id",
                MessageText = "Test notification",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                MessageType = MessageType.General,
                Sender = TestDataHelper.CreateTestUser("sender-id")
            }
        };

            MockUnitOfWork.Setup(x => x.InvestmentMessage.GetMessagesByRecipientId("test-user-id", false))
                .ReturnsAsync(messages);

            // Act
            var result = await _controller.GetNotificationHistory(
                GetValidAuthorizationHeader(), null, null, null, null, 1, 20);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
