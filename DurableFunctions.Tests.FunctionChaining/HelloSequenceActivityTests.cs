using Microsoft.Azure.WebJobs;
using Xunit;
using Moq;
using DurableFunctions.FunctionChaining;
using Microsoft.Extensions.Logging;

namespace DurableFunctions.Tests.FunctionChaining
{
    public class HelloSequenceActivityTests
    {
        [Fact]
        public void SayHello_returns_greeting()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            var durableActivityContextMock = new Mock<DurableActivityContextBase>();
            durableActivityContextMock.Setup(x => x.GetInput<string>()).Returns("John");

            // Act
            var result = new HelloSequence().SayHello(durableActivityContextMock.Object, loggerMock.Object);

            // Assert
            Assert.Equal("Hello John!", result);
        }
    }
}
