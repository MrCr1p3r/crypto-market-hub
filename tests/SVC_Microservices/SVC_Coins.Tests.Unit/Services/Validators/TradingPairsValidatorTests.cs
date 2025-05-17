using System.Globalization;
using SharedLibrary.Enums;
using SharedLibrary.Errors;
using SVC_Coins.ApiContracts.Requests;
using SVC_Coins.Domain.Entities;
using SVC_Coins.Repositories.Interfaces;
using SVC_Coins.Services.Validators;

namespace SVC_Coins.Tests.Unit.Services.Validators;

public class TradingPairsValidatorTests
{
    private readonly Mock<ICoinsRepository> _coinsRepositoryMock;
    private readonly Mock<IExchangesRepository> _exchangesRepositoryMock;
    private readonly TradingPairsValidator _validator;

    public TradingPairsValidatorTests()
    {
        _coinsRepositoryMock = new Mock<ICoinsRepository>();
        _exchangesRepositoryMock = new Mock<IExchangesRepository>();
        _validator = new TradingPairsValidator(
            _coinsRepositoryMock.Object,
            _exchangesRepositoryMock.Object
        );

        _exchangesRepositoryMock
            .Setup(repo => repo.GetAllExchanges())
            .ReturnsAsync(TestData.Exchanges);

        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(It.IsAny<HashSet<int>>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task ValidateTradingPairsReplacementRequests_WhenRequestIsValid_ReturnsOk()
    {
        // Arrange
        var requests = TestData.ValidTradingPairRequests;
        var expectedCoinIds = new HashSet<int> { 1, 2, 3 }; // Combine all unique IDs from requests

        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(expectedCoinIds))
            .ReturnsAsync([]); // Ensure this specific set is checked and returns empty

        // Act
        var result = await _validator.ValidateTradingPairsReplacementRequests(requests);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTradingPairsReplacementRequests_WhenMissingCoinId_ReturnsFail()
    {
        // Arrange
        const int missingCoinId = 999;
        var requests = TestData.GetRequestsWithMissingCoinId(missingCoinId);
        var expectedCoinIds = new HashSet<int> { 1, missingCoinId };
        var missingIdsResult = new HashSet<int> { missingCoinId };

        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(expectedCoinIds))
            .ReturnsAsync(missingIdsResult);

        // Act
        var result = await _validator.ValidateTradingPairsReplacementRequests(requests);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains(missingCoinId.ToString(CultureInfo.InvariantCulture))
            );
        result.Errors[0].Should().BeOfType<GenericErrors.BadRequestError>();
    }

    [Fact]
    public async Task ValidateTradingPairsReplacementRequests_WhenInvalidExchange_ReturnsFail()
    {
        // Arrange
        var invalidExchange = TestData.InvalidExchange;
        var requests = TestData.GetRequestsWithInvalidExchange(invalidExchange);
        var expectedCoinIds = new HashSet<int> { 1, 2 }; // IDs from the valid request

        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(expectedCoinIds))
            .ReturnsAsync([]); // Assume coins exist

        // Act
        var result = await _validator.ValidateTradingPairsReplacementRequests(requests);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error => error.Message.Contains(invalidExchange.ToString()));
        result.Errors[0].Should().BeOfType<GenericErrors.BadRequestError>();
    }

    [Fact]
    public async Task ValidateTradingPairsReplacementRequests_WhenMultipleErrors_ReturnsCombinedFail()
    {
        // Arrange
        const int missingCoinId = 888;
        var invalidExchange = TestData.InvalidExchange;
        var requests = TestData.GetRequestsWithMultipleErrors(missingCoinId, invalidExchange);
        var expectedCoinIds = new HashSet<int> { 1, missingCoinId, 3 }; // Combined IDs
        var missingIdsResult = new HashSet<int> { missingCoinId };

        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(expectedCoinIds))
            .ReturnsAsync(missingIdsResult);

        // Act
        var result = await _validator.ValidateTradingPairsReplacementRequests(requests);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1); // Combined into one BadRequestError
        var errorMessage = result.Errors[0].Message;
        errorMessage.Should().Contain(missingCoinId.ToString(CultureInfo.InvariantCulture));
        errorMessage.Should().Contain(invalidExchange.ToString());
        result.Errors[0].Should().BeOfType<GenericErrors.BadRequestError>();
    }

    private static class TestData
    {
        public static readonly Exchange InvalidExchange = (Exchange)999;

        public static readonly IEnumerable<TradingPairCreationRequest> ValidTradingPairRequests =
        [
            new()
            {
                IdCoinMain = 1,
                IdCoinQuote = 2,
                Exchanges = [Exchange.Binance],
            },
            new()
            {
                IdCoinMain = 3,
                IdCoinQuote = 1,
                Exchanges = [Exchange.Bybit, Exchange.Binance],
            },
        ];

        public static IEnumerable<TradingPairCreationRequest> GetRequestsWithMissingCoinId(
            int missingId
        ) =>
            [
                new()
                {
                    IdCoinMain = 1,
                    IdCoinQuote = missingId,
                    Exchanges = [Exchange.Binance],
                },
            ];

        public static IEnumerable<TradingPairCreationRequest> GetRequestsWithInvalidExchange(
            Exchange invalidExchange
        ) =>
            [
                new()
                {
                    IdCoinMain = 1,
                    IdCoinQuote = 2,
                    Exchanges = [Exchange.Binance, invalidExchange],
                },
            ];

        public static IEnumerable<TradingPairCreationRequest> GetRequestsWithMultipleErrors(
            int missingId,
            Exchange invalidExchange
        ) =>
            [
                new()
                {
                    IdCoinMain = 1,
                    IdCoinQuote = missingId,
                    Exchanges = [Exchange.Binance],
                }, // Missing quote coin
                new()
                {
                    IdCoinMain = 3,
                    IdCoinQuote = 1,
                    Exchanges = [invalidExchange],
                }, // Invalid exchange
            ];

        public static readonly IEnumerable<ExchangesEntity> Exchanges =
        [
            new ExchangesEntity { Id = 1, Name = "Binance" },
            new ExchangesEntity { Id = 2, Name = "Bybit" },
        ];
    }
}
