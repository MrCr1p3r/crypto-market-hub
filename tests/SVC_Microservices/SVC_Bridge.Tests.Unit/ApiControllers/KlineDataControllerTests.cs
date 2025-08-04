using FluentResults;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Errors;
using SVC_Bridge.ApiContracts.Responses.KlineData;
using SVC_Bridge.ApiControllers;
using SVC_Bridge.Services.Interfaces;

namespace SVC_Bridge.Tests.Unit.ApiControllers;

public class KlineDataControllerTests
{
    private readonly Mock<IKlineDataService> _mockKlineDataService;
    private readonly KlineDataController _testedController;

    public KlineDataControllerTests()
    {
        _mockKlineDataService = new Mock<IKlineDataService>();
        _testedController = new KlineDataController(_mockKlineDataService.Object);
    }

    [Fact]
    public async Task UpdateKlineData_OnSuccess_CallsServiceAndReturnsOkWithData()
    {
        // Arrange
        var expectedKlineData = TestData.SampleKlineDataCollection;
        var successResult = Result.Ok(expectedKlineData);

        _mockKlineDataService
            .Setup(service => service.UpdateKlineData())
            .ReturnsAsync(successResult);

        // Act
        var result = await _testedController.UpdateKlineData();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(expectedKlineData);

        _mockKlineDataService.Verify(service => service.UpdateKlineData(), Times.Once);
        _mockKlineDataService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateKlineData_OnSuccessWithEmptyData_CallsServiceAndReturnsOkWithEmptyCollection()
    {
        // Arrange
        var emptyResult = Result.Ok(Enumerable.Empty<KlineDataResponse>());

        _mockKlineDataService.Setup(service => service.UpdateKlineData()).ReturnsAsync(emptyResult);

        // Act
        var result = await _testedController.UpdateKlineData();

        // Assert
        result
            .Should()
            .BeOfType<OkObjectResult>()
            .Which.Value.Should()
            .BeEquivalentTo(Array.Empty<KlineDataResponse>());

        _mockKlineDataService.Verify(service => service.UpdateKlineData(), Times.Once);
        _mockKlineDataService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateKlineData_OnInternalError_CallsServiceAndReturnsInternalServerError()
    {
        // Arrange
        var errorMessage = "External service unavailable";
        var failureResult = Result.Fail<IEnumerable<KlineDataResponse>>(
            new GenericErrors.InternalError(errorMessage)
        );

        _mockKlineDataService
            .Setup(service => service.UpdateKlineData())
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.UpdateKlineData();

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);

        var problemDetails = result
            .Should()
            .BeOfType<ObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Subject;

        problemDetails.Detail.Should().Contain(errorMessage);

        _mockKlineDataService.Verify(service => service.UpdateKlineData(), Times.Once);
        _mockKlineDataService.VerifyNoOtherCalls();
    }

    private static class TestData
    {
        public static readonly IEnumerable<KlineDataResponse> SampleKlineDataCollection =
        [
            new()
            {
                IdTradingPair = 1,
                Klines =
                [
                    new()
                    {
                        OpenTime = 1640995200000, // 2022-01-01 00:00:00 UTC
                        OpenPrice = "47000",
                        HighPrice = "48000",
                        LowPrice = "46000",
                        ClosePrice = "47500",
                        Volume = "1000",
                        CloseTime = 1641081599999, // 2022-01-01 23:59:59 UTC
                    },
                    new()
                    {
                        OpenTime = 1641081600000, // 2022-01-02 00:00:00 UTC
                        OpenPrice = "47500",
                        HighPrice = "49000",
                        LowPrice = "47000",
                        ClosePrice = "48500",
                        Volume = "1200",
                        CloseTime = 1641167999999, // 2022-01-02 23:59:59 UTC
                    },
                ],
            },
            new()
            {
                IdTradingPair = 2,
                Klines =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = "3700",
                        HighPrice = "3800",
                        LowPrice = "3600",
                        ClosePrice = "3750",
                        Volume = "500",
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = "3750",
                        HighPrice = "3900",
                        LowPrice = "3700",
                        ClosePrice = "3850",
                        Volume = "600",
                        CloseTime = 1641167999999,
                    },
                ],
            },
            new()
            {
                IdTradingPair = 3,
                Klines =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = "1.2",
                        HighPrice = "1.3",
                        LowPrice = "1.1",
                        ClosePrice = "1.25",
                        Volume = "10000",
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = "1.25",
                        HighPrice = "1.35",
                        LowPrice = "1.2",
                        ClosePrice = "1.3",
                        Volume = "12000",
                        CloseTime = 1641167999999,
                    },
                ],
            },
        ];
    }
}
