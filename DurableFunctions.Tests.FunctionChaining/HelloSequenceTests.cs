using DurableFunctions.FunctionChaining;
using Microsoft.Azure.WebJobs;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace DurableFunctions.Tests.FunctionChaining
{
    public class HelloSequenceTests
    {
        [Fact]
        public async Task Should_return_multiple_greetings()
        {
            // Arrange
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>("HelloSequence_Hello", "John")).ReturnsAsync("Hello John!");
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>("HelloSequence_Hello", "Peter")).ReturnsAsync("Hello Peter!");
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>("HelloSequence_Hello", "Chris")).ReturnsAsync("Hello Chris!");

            // Act
            var result = await new HelloSequence().RunOrchestrator(durableOrchestrationContextMock.Object);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Hello John!", result[0]);
            Assert.Equal("Hello Peter!", result[1]);
            Assert.Equal("Hello Chris!", result[2]);
        }
    }
}
