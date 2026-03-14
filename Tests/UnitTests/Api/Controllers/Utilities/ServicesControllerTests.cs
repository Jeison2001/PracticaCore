using Api.Controllers.Utilities;
using Api.Responses;
using Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Tests.UnitTests.Api.Controllers.Utilities
{
    public class ServicesControllerTests
    {
        private readonly Mock<IIdGeneratorService> _mockIdGeneratorService;
        private readonly ServicesController _controller;

        public ServicesControllerTests()
        {
            _mockIdGeneratorService = new Mock<IIdGeneratorService>();
            _controller = new ServicesController(_mockIdGeneratorService.Object);
        }

        [Fact]
        public void GenerateId_ReturnsOkResult_WithIds()
        {
            // Arrange
            var expectedUniqueId = "123456";
            var expectedGuid = Guid.NewGuid();

            _mockIdGeneratorService.Setup(s => s.GenerateUniqueId())
                .Returns(expectedUniqueId);
            _mockIdGeneratorService.Setup(s => s.GenerateGuid())
                .Returns(expectedGuid);

            // Act
            var result = _controller.GenerateId();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(apiResponse.Success);
            
            // Use reflection or dynamic to check the anonymous object properties if needed, 
            // or just verify the mock calls were made.
            // Since the Data is an anonymous object, we can inspect it via reflection or serialization.
            // For this unit test, verifying the mock interaction and the response type is often sufficient,
            // but let's try to verify the data content too.
            
            var data = apiResponse.Data;
            Assert.NotNull(data);
            
            var uniqueIdProp = data.GetType().GetProperty("UniqueId");
            var guidProp = data.GetType().GetProperty("Guid");
            
            Assert.NotNull(uniqueIdProp);
            Assert.NotNull(guidProp);
            
            Assert.Equal(expectedUniqueId, uniqueIdProp.GetValue(data));
            Assert.Equal(expectedGuid, guidProp.GetValue(data));
        }
    }
}
