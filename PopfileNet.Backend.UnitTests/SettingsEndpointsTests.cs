using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using PopfileNet.Backend.Groups;
using PopfileNet.Backend.Models;
using PopfileNet.Common;
using Xunit;

namespace PopfileNet.Backend.UnitTests
{
    public class SettingsEndpointsTests
    {
        [Fact]
        public async Task TestConnection_ReturnsBadRequest_WhenImapNotConfigured()
        {
            // arrange: mock a service that reports no configuration
            var imapMock = new Mock<IImapService>(MockBehavior.Strict);
            imapMock.Setup(s => s.IsConfiguredAsync(default)).ReturnsAsync(false);

            // act: call the handler directly without spinning up a server
            var result = await SettingsGroupExtensions.TestConnectionAsync(imapMock.Object);

            // assert
            Assert.IsType<BadRequest<ApiResponse<bool>>>(result);
            var bad = ((BadRequest<ApiResponse<bool>>)result).Value;
            bad.Should().NotBeNull();
            bad.IsSuccess.Should().BeFalse();
            bad.Error?.Message.Should().Contain("IMAP settings are not configured");
        }
    }
}
