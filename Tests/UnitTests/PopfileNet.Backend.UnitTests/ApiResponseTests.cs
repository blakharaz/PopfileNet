using Shouldly;
using PopfileNet.Backend.Models;
using Xunit;

namespace PopfileNet.Backend.UnitTests
{
    public class ApiResponseTests
    {
        [Fact]
        public void Success_ReturnsSuccessfulResponse_WithValue()
        {
            // Arrange
            var value = "test value";

            // Act
            var response = ApiResponse<string>.Success(value);

            // Assert
            response.IsSuccess.ShouldBeTrue();
            response.Value.ShouldBe(value);
            response.Error.ShouldBeNull();
        }

        [Fact]
        public void Failure_ReturnsFailedResponse_WithError()
        {
            // Arrange
            var code = "TEST_ERROR";
            var message = "Test error message";

            // Act
            var response = ApiResponse<string>.Failure(code, message);

            // Assert
            response.IsSuccess.ShouldBeFalse();
            response.Error.ShouldNotBeNull();
            response.Error!.Code.ShouldBe(code);
            response.Error!.Message.ShouldBe(message);
            response.Value.ShouldBeNull();
        }

        [Fact]
        public void IsSuccess_TrueWhenErrorIsNull()
        {
            // Arrange
            var response = new ApiResponse<string> { Value = "test" };

            // Act & Assert
            response.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void IsSuccess_FalseWhenErrorIsNotNull()
        {
            // Arrange
            var response = new ApiResponse<string> { Error = new ApiError("TEST", "test") };

            // Act & Assert
            response.IsSuccess.ShouldBeFalse();
        }
    }
}