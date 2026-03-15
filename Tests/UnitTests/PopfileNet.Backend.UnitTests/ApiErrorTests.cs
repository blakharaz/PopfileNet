using Shouldly;
using PopfileNet.Backend.Models;
using System.Reflection;
using Xunit;

namespace PopfileNet.Backend.UnitTests
{
    public class ApiErrorTests
    {
        [Fact]
        public void Constructor_SetsCodeAndMessageCorrectly()
        {
            // Arrange
            var code = "ERROR_CODE";
            var message = "Error description";

            // Act
            var error = new ApiError(code, message);

            // Assert
            error.Code.ShouldBe(code);
            error.Message.ShouldBe(message);
        }

        [Fact]
        public void Properties_AreReadOnly()
        {
            // Arrange
            var code = "TEST";
            var message = "Test message";
            var error = new ApiError(code, message);

            // Assert - Verify properties are read-only (no setter)
            var codeProperty = typeof(ApiError).GetProperty(nameof(ApiError.Code));
            codeProperty.ShouldNotBeNull();
            codeProperty.SetMethod.ShouldBeNull();

            var messageProperty = typeof(ApiError).GetProperty(nameof(ApiError.Message));
            messageProperty.ShouldNotBeNull();
            messageProperty.SetMethod.ShouldBeNull();
            
            // Also verify the values are correct
            error.Code.ShouldBe(code);
            error.Message.ShouldBe(message);
        }

        [Fact]
        public void CanCreateDifferentInstances_WithSameValues()
        {
            // Arrange
            var code = "SAME_CODE";
            var message = "SAME_MESSAGE";

            // Act
            var error1 = new ApiError(code, message);
            var error2 = new ApiError(code, message);

            // Assert
            error1.ShouldNotBeSameAs(error2); // Different instances
            error1.Code.ShouldBe(error2.Code);
            error1.Message.ShouldBe(error2.Message);
        }
    }
}