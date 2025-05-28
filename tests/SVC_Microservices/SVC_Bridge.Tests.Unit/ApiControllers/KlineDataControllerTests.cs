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
    public async Task UpdateKlineData_OnBadRequestError_CallsServiceAndReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Invalid request parameters";
        var failureResult = Result.Fail<IEnumerable<KlineDataResponse>>(
            new GenericErrors.BadRequestError(errorMessage)
        );

        _mockKlineDataService
            .Setup(service => service.UpdateKlineData())
            .ReturnsAsync(failureResult);

        // Act
        var result = await _testedController.UpdateKlineData();

        // Assert
        result
            .Should()
            .BeOfType<BadRequestObjectResult>()
            .Which.Value.Should()
            .BeOfType<ProblemDetails>()
            .Which.Detail.Should()
            .Contain(errorMessage);

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
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000, // 2022-01-01 00:00:00 UTC
                        OpenPrice = 47000m,
                        HighPrice = 48000m,
                        LowPrice = 46000m,
                        ClosePrice = 47500m,
                        Volume = 1000m,
                        CloseTime = 1641081599999, // 2022-01-01 23:59:59 UTC
                    },
                    new()
                    {
                        OpenTime = 1641081600000, // 2022-01-02 00:00:00 UTC
                        OpenPrice = 47500m,
                        HighPrice = 49000m,
                        LowPrice = 47000m,
                        ClosePrice = 48500m,
                        Volume = 1200m,
                        CloseTime = 1641167999999, // 2022-01-02 23:59:59 UTC
                    },
                ],
            },
            new()
            {
                IdTradingPair = 2,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 3700m,
                        HighPrice = 3800m,
                        LowPrice = 3600m,
                        ClosePrice = 3750m,
                        Volume = 500m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 3750m,
                        HighPrice = 3900m,
                        LowPrice = 3700m,
                        ClosePrice = 3850m,
                        Volume = 600m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
            new()
            {
                IdTradingPair = 3,
                KlineData =
                [
                    new()
                    {
                        OpenTime = 1640995200000,
                        OpenPrice = 1.2m,
                        HighPrice = 1.3m,
                        LowPrice = 1.1m,
                        ClosePrice = 1.25m,
                        Volume = 10000m,
                        CloseTime = 1641081599999,
                    },
                    new()
                    {
                        OpenTime = 1641081600000,
                        OpenPrice = 1.25m,
                        HighPrice = 1.35m,
                        LowPrice = 1.2m,
                        ClosePrice = 1.3m,
                        Volume = 12000m,
                        CloseTime = 1641167999999,
                    },
                ],
            },
        ];
    }
}
