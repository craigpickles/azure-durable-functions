using DurableFunctions.FunctionChaining;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace DurableFunctions.Tests.FunctionChaining
{
    public class HttpStartTests
    {
        [Fact]
        public async Task Should_return_status_response_content()
        {
            // Arrange
            const string instanceId = "7E467BDB-213F-407A-B86A-1954053D3C24";

            var loggerMock = new Mock<ILogger>();

            var durableOrchestrationClientBaseMock = new Mock<DurableOrchestrationClientBase>();

            durableOrchestrationClientBaseMock.
                Setup(x => x.StartNewAsync("HelloSequence", It.IsAny<object>())).
                ReturnsAsync(instanceId);

            durableOrchestrationClientBaseMock
                .Setup(x => x.CreateCheckStatusResponse(It.IsAny<HttpRequestMessage>(), instanceId))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Check Status Response")
                });

            // Act
            var result = await new HelloSequence().HttpStart(
                new HttpRequestMessage()
                {
                    RequestUri = new Uri("http://localhost:7071/api/MyFunction_HttpStart"),
                },
                durableOrchestrationClientBaseMock.Object,
                loggerMock.Object);
            var responseContent = await result.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal("Check Status Response", responseContent);
        }
    }
}
