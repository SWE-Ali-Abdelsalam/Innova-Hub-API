using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Tests.Integration
{
    public class ModelValidationTests
    {
        [Fact]
        public void LoginDTO_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var loginDto = new InnoHub.ModelDTO.LoginDTO
            {
                Email = "test@example.com",
                Password = "ValidPassword123!"
            };

            // Act
            var validationResults = ValidateModel(loginDto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void LoginDTO_WithInvalidEmail_ShouldFailValidation()
        {
            // Arrange
            var loginDto = new InnoHub.ModelDTO.LoginDTO
            {
                Email = "invalid-email",
                Password = "ValidPassword123!"
            };

            // Act
            var validationResults = ValidateModel(loginDto);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void AddToCartDTO_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var addToCartDto = new InnoHub.ModelDTO.AddToCartDTO
            {
                ProductId = 1,
                Quantity = 2
            };

            // Act
            var validationResults = ValidateModel(addToCartDto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void AddToCartDTO_WithInvalidQuantity_ShouldFailValidation()
        {
            // Arrange
            var addToCartDto = new InnoHub.ModelDTO.AddToCartDTO
            {
                ProductId = 1,
                Quantity = 0
            };

            // Act
            var validationResults = ValidateModel(addToCartDto);

            // Assert
            validationResults.Should().NotBeEmpty();
        }

        private static IList<System.ComponentModel.DataAnnotations.ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(model, null, null);
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }
    }
}
