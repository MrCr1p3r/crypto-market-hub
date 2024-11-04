using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SVC_Kline.Controllers;
using SVC_Kline.Models.Input;
using SVC_Kline.Repositories.Interfaces;

namespace SVC_Kline.Tests.Controllers;

public class KlineDataControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<IKlineDataRepository> _mockRepository;
    private readonly KlineDataController _controller;

    public KlineDataControllerTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _mockRepository = new Mock<IKlineDataRepository>();
        _controller = new KlineDataController(_mockRepository.Object);
    }

    [Fact]
    public async Task InsertKlineData_CallsRepository()
    {
        // Arrange
        var klineData = _fixture.Create<KlineData>();

        // Act
        await _controller.InsertKlineData(klineData);

        // Assert
        _mockRepository.Verify(repo => repo.InsertKlineData(klineData), Times.Once);
    }

    [Fact]
    public async Task InsertKlineData_ValidData_ReturnsOkResult()
    {
        // Arrange
        var klineData = _fixture.Create<KlineData>();
        
        // Act
        var result = await _controller.InsertKlineData(klineData);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Kline data inserted successfully.", okResult.Value);
    }
}
