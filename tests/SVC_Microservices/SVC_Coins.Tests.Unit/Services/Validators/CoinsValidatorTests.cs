using System.Globalization;
using FluentAssertions;
using Moq;
using SharedLibrary.Enums;
using SharedLibrary.Errors;
using SVC_Coins.ApiModels.Requests;
using SVC_Coins.ApiModels.Requests.CoinCreation;
using SVC_Coins.Domain.Entities;
using SVC_Coins.Domain.ValueObjects;
using SVC_Coins.Repositories.Interfaces;
using SVC_Coins.Services.Validators;

namespace SVC_Coins.Tests.Unit.Services.Validators;

public class CoinsValidatorTests
{
    private readonly Mock<ICoinsRepository> _coinsRepositoryMock;
    private readonly Mock<IExchangesRepository> _exchangesRepositoryMock;
    private readonly CoinsValidator _validator;

    public CoinsValidatorTests()
    {
        _coinsRepositoryMock = new Mock<ICoinsRepository>();
        _exchangesRepositoryMock = new Mock<IExchangesRepository>();
        _validator = new CoinsValidator(
            _coinsRepositoryMock.Object,
            _exchangesRepositoryMock.Object
        );

        _exchangesRepositoryMock
            .Setup(repo => repo.GetAllExchanges())
            .ReturnsAsync(TestData.SampleExchanges);
    }

    [Fact]
    public async Task ValidateCoinCreationRequests_WhenRequestIsValid_ReturnsOk()
    {
        // Arrange
        var requests = TestData.ValidCoinCreationRequests;
        var quoteCoinIds = new HashSet<int> { 2 }; // ID from TestData.ValidCoinCreationRequests

        _coinsRepositoryMock
            .Setup(repo =>
                repo.GetCoinsBySymbolNamePairs(It.IsAny<IEnumerable<CoinSymbolNamePair>>())
            )
            .ReturnsAsync([]); // No duplicates
        _coinsRepositoryMock.Setup(repo => repo.GetMissingCoinIds(quoteCoinIds)).ReturnsAsync([]); // No missing Ids
        _exchangesRepositoryMock
            .Setup(repo => repo.GetAllExchanges())
            .ReturnsAsync(TestData.SampleExchanges); // Valid exchanges

        // Act
        var result = await _validator.ValidateCoinCreationRequests(requests);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateCoinCreationRequests_WhenDuplicateMainCoinExists_ReturnsFail()
    {
        // Arrange
        var requests = TestData.RequestWithExistingBtc;
        var existingBtc = TestData.BtcEntity;

        _coinsRepositoryMock
            .Setup(repo =>
                repo.GetCoinsBySymbolNamePairs(
                    It.Is<IEnumerable<CoinSymbolNamePair>>(coinPairs =>
                        coinPairs.Any(pair => pair.Symbol == "BTC")
                    )
                )
            )
            .ReturnsAsync([existingBtc]);
        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(It.IsAny<HashSet<int>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _validator.ValidateCoinCreationRequests(requests);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains(existingBtc.Name)
                && error.Message.Contains(existingBtc.Symbol)
            );
        result.Errors[0].Should().BeOfType<GenericErrors.BadRequestError>();
    }

    [Fact]
    public async Task ValidateCoinCreationRequests_WhenDuplicateQuoteCoinExists_ReturnsFail()
    {
        // Arrange
        var requests = TestData.RequestWithNewQuoteUsdt;
        var existingUsdt = TestData.UsdtEntity;

        _coinsRepositoryMock
            .Setup(repo =>
                repo.GetCoinsBySymbolNamePairs(
                    It.Is<IEnumerable<CoinSymbolNamePair>>(coinPairs =>
                        coinPairs.Any(pair => pair.Symbol == "USDT")
                    )
                )
            )
            .ReturnsAsync([existingUsdt]); // Simulate USDT duplicate
        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(It.IsAny<HashSet<int>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _validator.ValidateCoinCreationRequests(requests);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains(existingUsdt.Name)
                && error.Message.Contains(existingUsdt.Symbol)
            );
        result.Errors[0].Should().BeOfType<GenericErrors.BadRequestError>();
    }

    [Fact]
    public async Task ValidateCoinCreationRequests_WhenNonExistingQuoteCoinId_ReturnsFail()
    {
        // Arrange
        const int nonExistingId = 999;
        var requests = TestData.GetRequestWithNonExistingQuoteId(nonExistingId);
        var missingIds = new HashSet<int> { nonExistingId };
        var expectedCheckedIds = new HashSet<int> { nonExistingId };

        _coinsRepositoryMock
            .Setup(repo =>
                repo.GetCoinsBySymbolNamePairs(It.IsAny<IEnumerable<CoinSymbolNamePair>>())
            )
            .ReturnsAsync([]); // No duplicates
        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(expectedCheckedIds))
            .ReturnsAsync(missingIds); // ID 999 is missing

        // Act
        var result = await _validator.ValidateCoinCreationRequests(requests);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains(nonExistingId.ToString(CultureInfo.InvariantCulture))
            );
        result.Errors[0].Should().BeOfType<GenericErrors.BadRequestError>();
    }

    [Fact]
    public async Task ValidateCoinCreationRequests_WhenInvalidExchange_ReturnsFail()
    {
        // Arrange
        var invalidExchange = TestData.InvalidExchange;
        var requests = TestData.GetRequestWithInvalidExchange(invalidExchange);
        var expectedQuoteId = new HashSet<int> { 2 }; // ID from the request's quote coin

        _coinsRepositoryMock
            .Setup(repo =>
                repo.GetCoinsBySymbolNamePairs(It.IsAny<IEnumerable<CoinSymbolNamePair>>())
            )
            .ReturnsAsync([]);
        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(expectedQuoteId))
            .ReturnsAsync([]); // Assume quote coin exists

        // Act
        var result = await _validator.ValidateCoinCreationRequests(requests);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error => error.Message.Contains(invalidExchange.ToString()));
        result.Errors[0].Should().BeOfType<GenericErrors.BadRequestError>();
    }

    [Fact]
    public async Task ValidateCoinCreationRequests_WhenMultipleErrors_ReturnsCombinedFail()
    {
        // Arrange
        var invalidExchange = TestData.InvalidExchange;
        const int missingQuoteId = 888;
        var requests = TestData.GetRequestWithMultipleErrors(missingQuoteId, invalidExchange);

        var existingBtc = TestData.BtcEntity;
        var missingIds = new HashSet<int> { missingQuoteId };
        var expectedCheckedIds = new HashSet<int> { missingQuoteId };

        _coinsRepositoryMock
            .Setup(repo =>
                repo.GetCoinsBySymbolNamePairs(It.IsAny<IEnumerable<CoinSymbolNamePair>>())
            )
            .ReturnsAsync([existingBtc]); // BTC is duplicate
        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(expectedCheckedIds))
            .ReturnsAsync(missingIds); // 888 is missing

        // Act
        var result = await _validator.ValidateCoinCreationRequests(requests);

        // Assert
        result.IsFailed.Should().BeTrue();
        var errorMessage = result.Errors[0].Message;
        errorMessage.Should().Contain(existingBtc.Name);
        errorMessage.Should().Contain(missingQuoteId.ToString(CultureInfo.InvariantCulture));
        errorMessage.Should().Contain(invalidExchange.ToString());
        result.Errors[0].Should().BeOfType<GenericErrors.BadRequestError>();
    }

    [Fact]
    public async Task ValidateMarketDataUpdateRequests_WhenAllIdsExist_ReturnsOk()
    {
        // Arrange
        var requests = TestData.ValidMarketDataUpdateRequests;
        var idsToCheck = new HashSet<int> { 1, 2 };

        _coinsRepositoryMock.Setup(repo => repo.GetMissingCoinIds(idsToCheck)).ReturnsAsync([]); // No missing IDs

        // Act
        var result = await _validator.ValidateMarketDataUpdateRequests(requests);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateMarketDataUpdateRequests_WhenSomeIdsAreMissing_ReturnsFail()
    {
        // Arrange
        const int missingId = 999;
        var requests = TestData.GetMarketDataUpdateRequestsWithMissingId(missingId);
        var idsToCheck = new HashSet<int> { 1, missingId };
        var missingIdsResult = new HashSet<int> { missingId };

        _coinsRepositoryMock
            .Setup(repo => repo.GetMissingCoinIds(idsToCheck))
            .ReturnsAsync(missingIdsResult); // 999 is missing

        // Act
        var result = await _validator.ValidateMarketDataUpdateRequests(requests);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.Should()
            .ContainSingle(error =>
                error.Message.Contains(missingId.ToString(CultureInfo.InvariantCulture))
            );
        result.Errors[0].Should().BeOfType<GenericErrors.NotFoundError>();
    }

    private static class TestData
    {
        public static readonly Exchange InvalidExchange = (Exchange)999;

        public static readonly List<CoinCreationRequest> ValidCoinCreationRequests =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                IdCoinGecko = "bitcoin",
                TradingPairs =
                [
                    // Quote coin with existing ID (Symbol/Name are placeholders)
                    new()
                    {
                        CoinQuote = new CoinCreationCoinQuote
                        {
                            Id = 2,
                            Symbol = "-",
                            Name = "-",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new()
            {
                Symbol = "ETH",
                Name = "Ethereum",
                Category = null,
                IdCoinGecko = "ethereum",
                TradingPairs =
                [
                    // Quote coin defined by Symbol/Name
                    new()
                    {
                        CoinQuote = new CoinCreationCoinQuote
                        {
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                            IdCoinGecko = "tether",
                        },
                        Exchanges = [Exchange.Binance, Exchange.Bybit],
                    },
                ],
            },
        ];

        public static readonly List<CoinCreationRequest> RequestWithExistingBtc =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs = [],
            },
        ]; // Empty trading pairs for simplicity

        public static readonly List<CoinCreationRequest> RequestWithNewQuoteUsdt =
        [
            new()
            {
                Symbol = "NEW",
                Name = "NewCoin",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new CoinCreationCoinQuote { Symbol = "USDT", Name = "Tether" },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
        ];

        public static List<CoinCreationRequest> GetRequestWithNonExistingQuoteId(int id) =>
            [
                new()
                {
                    Symbol = "BTC",
                    Name = "Bitcoin",
                    TradingPairs =
                    [
                        // Quote coin with non-existing ID (Symbol/Name are placeholders)
                        new()
                        {
                            CoinQuote = new CoinCreationCoinQuote
                            {
                                Id = id,
                                Symbol = "-",
                                Name = "-",
                            },
                            Exchanges = [Exchange.Binance],
                        },
                    ],
                },
            ];

        public static List<CoinCreationRequest> GetRequestWithInvalidExchange(
            Exchange invalidExchange
        ) =>
            [
                new()
                {
                    Symbol = "BTC",
                    Name = "Bitcoin",
                    TradingPairs =
                    [
                        // Quote coin with existing ID (Symbol/Name are placeholders)
                        new()
                        {
                            CoinQuote = new CoinCreationCoinQuote
                            {
                                Id = 2,
                                Symbol = "-",
                                Name = "-",
                            },
                            Exchanges = [invalidExchange],
                        },
                    ],
                },
            ];

        public static List<CoinCreationRequest> GetRequestWithMultipleErrors(
            int missingId,
            Exchange invalidExchange
        ) =>
            [
                new()
                {
                    Symbol = "BTC",
                    Name = "Bitcoin",
                    TradingPairs = [],
                }, // Duplicate main coin (empty pairs for simplicity)
                new()
                {
                    Symbol = "ETH",
                    Name = "Ethereum",
                    TradingPairs =
                    [
                        // Missing quote ID and invalid exchange (Symbol/Name are placeholders)
                        new()
                        {
                            CoinQuote = new CoinCreationCoinQuote
                            {
                                Id = missingId,
                                Symbol = "-",
                                Name = "-",
                            },
                            Exchanges = [invalidExchange],
                        },
                    ],
                },
            ];

        public static readonly List<CoinMarketDataUpdateRequest> ValidMarketDataUpdateRequests =
        [
            new() { Id = 1 },
            new() { Id = 2 },
        ];

        public static List<CoinMarketDataUpdateRequest> GetMarketDataUpdateRequestsWithMissingId(
            int missingId
        ) => [new() { Id = 1 }, new() { Id = missingId }];

        public static readonly CoinsEntity BtcEntity = new() { Symbol = "BTC", Name = "Bitcoin" };
        public static readonly CoinsEntity UsdtEntity = new() { Symbol = "USDT", Name = "Tether" };

        public static readonly IEnumerable<ExchangesEntity> SampleExchanges =
        [
            new ExchangesEntity { Id = 1, Name = "Binance" },
            new ExchangesEntity { Id = 2, Name = "Bybit" },
        ];
    }
}
