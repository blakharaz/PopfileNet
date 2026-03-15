using Shouldly;
using PopfileNet.Backend.Models;
using Xunit;

namespace PopfileNet.Backend.UnitTests
{
    public class PagedApiResponseTests
    {
        [Fact]
        public void Success_ReturnsSuccessfulPagedResponse_WithCorrectProperties()
        {
            // Arrange
            var items = new[] { "item1", "item2" };
            var page = 2;
            var pageSize = 10;
            var totalCount = 25;

            // Act
            var response = PagedApiResponse<string>.Success(items, page, pageSize, totalCount);

            // Assert
            response.IsSuccess.ShouldBeTrue();
            response.Items.ShouldBeSameAs(items);
            response.Page.ShouldBe(page);
            response.PageSize.ShouldBe(pageSize);
            response.TotalCount.ShouldBe(totalCount);
            response.Error.ShouldBeNull();
        }

        [Fact]
        public void Failure_ReturnsFailedPagedResponse_WithError()
        {
            // Arrange
            var code = "PAGED_ERROR";
            var message = "Test paged error";

            // Act
            var response = PagedApiResponse<string>.Failure(code, message);

            // Assert
            response.IsSuccess.ShouldBeFalse();
            response.Error.ShouldNotBeNull();
            response.Error!.Code.ShouldBe(code);
            response.Error!.Message.ShouldBe(message);
            response.Items.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(0, 10, 0)] // Empty collection
        [InlineData(5, 10, 1)] // Less than one page
        [InlineData(10, 10, 1)] // Exactly one page
        [InlineData(11, 10, 2)] // More than one page
        [InlineData(25, 10, 3)] // Multiple pages with remainder
        public void TotalPages_CalculatesCorrectly(int totalCount, int pageSize, int expectedPages)
        {
            // Arrange
            var response = PagedApiResponse<string>.Success([], 1, pageSize, totalCount);

            // Act & Assert
            response.TotalPages.ShouldBe(expectedPages);
        }

        [Fact]
        public void HasPrevious_TrueWhenPageGreaterThanOne()
        {
            // Arrange
            var response = PagedApiResponse<string>.Success([], 2, 10, 100);

            // Act & Assert
            response.HasPrevious.ShouldBeTrue();
        }

        [Fact]
        public void HasPrevious_FalseWhenPageIsOne()
        {
            // Arrange
            var response = PagedApiResponse<string>.Success([], 1, 10, 100);

            // Act & Assert
            response.HasPrevious.ShouldBeFalse();
        }

        [Fact]
        public void HasNext_TrueWhenPageLessThanTotalPages()
        {
            // Arrange
            var response = PagedApiResponse<string>.Success([], 1, 10, 25); // 3 pages total

            // Act & Assert
            response.HasNext.ShouldBeTrue();
        }

        [Fact]
        public void HasNext_FalseWhenPageEqualsTotalPages()
        {
            // Arrange
            var response = PagedApiResponse<string>.Success([], 3, 10, 25); // 3 pages total

            // Act & Assert
            response.HasNext.ShouldBeFalse();
        }
    }
}