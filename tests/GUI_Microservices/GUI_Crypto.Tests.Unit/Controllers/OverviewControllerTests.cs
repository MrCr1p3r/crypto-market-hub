using AutoFixture;
using FluentAssertions;
using GUI_Crypto.Controllers;
using GUI_Crypto.ViewModels;
using GUI_Crypto.ViewModels.Factories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GUI_Crypto.Tests.Unit.Controllers;

public class OverviewControllerTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ICryptoViewModelFactory> _viewModelFactoryMock;
    private readonly OverviewController _controller;

    public OverviewControllerTests()
    {
        _fixture = new Fixture();
        _viewModelFactoryMock = new Mock<ICryptoViewModelFactory>();
        _controller = new OverviewController(_viewModelFactoryMock.Object);
    }

    [Fact]
    public async Task Overview_ShouldCallCreateCoinViewModel()
    {
        // Arrange
        var expectedViewModel = _fixture.Create<OverviewViewModel>();
        _viewModelFactoryMock
            .Setup(factory => factory.CreateOverviewViewModel())
            .ReturnsAsync(expectedViewModel);

        // Act
        await _controller.Overview();

        // Assert
        _viewModelFactoryMock.Verify(factory => factory.CreateOverviewViewModel(), Times.Once);
    }

    [Fact]
    public async Task Overview_ShouldReturnViewWithExpectedViewModel()
    {
        // Arrange
        var expectedViewModel = _fixture.Create<OverviewViewModel>();
        _viewModelFactoryMock
            .Setup(factory => factory.CreateOverviewViewModel())
            .ReturnsAsync(expectedViewModel);

        // Act
        var result = await _controller.Overview();

        // Assert
        var viewResult = result as ViewResult;
        viewResult!.ViewName.Should().Be("Overview");
        viewResult.Model.Should().BeEquivalentTo(expectedViewModel);
    }
}
