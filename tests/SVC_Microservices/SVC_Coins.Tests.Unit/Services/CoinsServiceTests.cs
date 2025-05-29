using FluentAssertions.ArgumentMatchers.Moq;
using FluentResults;
using SharedLibrary.Enums;
using SharedLibrary.Errors;
using SVC_Coins.ApiContracts.Requests;
using SVC_Coins.ApiContracts.Requests.CoinCreation;
using SVC_Coins.ApiContracts.Responses;
using SVC_Coins.Domain.Entities;
using SVC_Coins.Domain.ValueObjects;
using SVC_Coins.Repositories.Interfaces;
using SVC_Coins.Services;
using SVC_Coins.Services.Validators.Interfaces;

namespace SVC_Coins.Tests.Unit.Services;

public class CoinsServiceTests
{
    private readonly Mock<ICoinsRepository> _coinsRepositoryMock;
    private readonly Mock<ITradingPairsRepository> _tradingPairsRepositoryMock;
    private readonly Mock<IExchangesRepository> _exchangesRepositoryMock;
    private readonly Mock<ICoinsValidator> _coinsValidatorMock;
    private readonly Mock<ITradingPairsValidator> _tradingPairsValidatorMock;
    private readonly CoinsService _testedService;

    public CoinsServiceTests()
    {
        _coinsRepositoryMock = new Mock<ICoinsRepository>();
        _tradingPairsRepositoryMock = new Mock<ITradingPairsRepository>();
        _exchangesRepositoryMock = new Mock<IExchangesRepository>();
        _coinsValidatorMock = new Mock<ICoinsValidator>();
        _tradingPairsValidatorMock = new Mock<ITradingPairsValidator>();

        _testedService = new CoinsService(
            _coinsRepositoryMock.Object,
            _tradingPairsRepositoryMock.Object,
            _exchangesRepositoryMock.Object,
            _coinsValidatorMock.Object,
            _tradingPairsValidatorMock.Object
        );
    }

    [Fact]
    public async Task GetAllCoins_Always_CallsRepositoryOnce()
    {
        // Arrange
        _coinsRepositoryMock.Setup(repo => repo.GetAllCoinsWithRelations()).ReturnsAsync([]);

        // Act
        await _testedService.GetAllCoins();

        // Assert
        _coinsRepositoryMock.Verify(repo => repo.GetAllCoinsWithRelations(), Times.Once);
    }

    [Fact]
    public async Task GetAllCoins_WhenCoinsExist_ReturnsAllCoins()
    {
        // Arrange
        _coinsRepositoryMock
            .Setup(repo => repo.GetAllCoinsWithRelations())
            .ReturnsAsync(TestData.CoinEntities);

        // Act
        var result = await _testedService.GetAllCoins();

        // Assert
        result.Should().BeEquivalentTo(TestData.ExpectedCoins);
    }

    [Fact]
    public async Task GetAllCoins_WhenNoCoinsExist_ReturnsEmptyCollection()
    {
        // Arrange
        _coinsRepositoryMock.Setup(repo => repo.GetAllCoinsWithRelations()).ReturnsAsync([]);

        // Act
        var result = await _testedService.GetAllCoins();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCoinsByIds_Always_CallsRepositoryOnce()
    {
        // Arrange
        var requestedIds = TestData.ExistingIds;
        _coinsRepositoryMock
            .Setup(repo => repo.GetCoinsByIdsWithRelations(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([]);

        // Act
        await _testedService.GetCoinsByIds(requestedIds);

        // Assert
        _coinsRepositoryMock.Verify(
            repo => repo.GetCoinsByIdsWithRelations(requestedIds),
            Times.Once
        );
    }

    [Fact]
    public async Task GetCoinsByIds_WhenIdsExist_ReturnsMatchingCoins()
    {
        // Arrange
        var requestedIds = TestData.ExistingIds;
        var expectedEntities = TestData.CoinEntities.Where(coin => requestedIds.Contains(coin.Id));
        var expectedCoins = TestData.ExpectedCoins.Where(coin => requestedIds.Contains(coin.Id));

        _coinsRepositoryMock
            .Setup(repo => repo.GetCoinsByIdsWithRelations(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result = await _testedService.GetCoinsByIds(requestedIds);

        // Assert
        result.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetCoinsByIds_WhenSomeIdsDoNotExist_ReturnsOnlyExistingCoins()
    {
        // Arrange
        var requestedIds = TestData.PartiallyExistingIds;
        var expectedEntities = TestData.CoinEntities.Where(coin => requestedIds.Contains(coin.Id));
        var expectedCoins = TestData.ExpectedCoins.Where(coin => requestedIds.Contains(coin.Id));

        _coinsRepositoryMock
            .Setup(repo => repo.GetCoinsByIdsWithRelations(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result = await _testedService.GetCoinsByIds(requestedIds);

        // Assert
        result.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetCoinsByIds_WhenNoIdsExist_ReturnsEmptyCollection()
    {
        // Arrange
        var requestedIds = TestData.NonExistingIds;
        _coinsRepositoryMock
            .Setup(repo => repo.GetCoinsByIdsWithRelations(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _testedService.GetCoinsByIds(requestedIds);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCoinsByIds_WhenInputIsEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        _coinsRepositoryMock
            .Setup(repo => repo.GetCoinsByIdsWithRelations(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _testedService.GetCoinsByIds([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCoinsWithTradingPairs_Always_CallsValidationOnce()
    {
        // Arrange
        var requests = TestData.CoinCreationRequests;

        _coinsValidatorMock
            .Setup(validator =>
                validator.ValidateCoinCreationRequests(It.IsAny<IEnumerable<CoinCreationRequest>>())
            )
            .ReturnsAsync(Result.Ok());

        // Act
        await _testedService.CreateCoinsWithTradingPairs(requests);

        // Assert
        _coinsValidatorMock.Verify(
            validator => validator.ValidateCoinCreationRequests(requests),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateCoinsWithTradingPairs_WhenValidationFails_ReturnsFailureResult()
    {
        // Arrange
        var validationFailureResult = Result.Fail(
            new GenericErrors.BadRequestError("Validation failed")
        );

        _coinsValidatorMock
            .Setup(validator =>
                validator.ValidateCoinCreationRequests(It.IsAny<IEnumerable<CoinCreationRequest>>())
            )
            .ReturnsAsync(validationFailureResult);

        // Act
        var result = await _testedService.CreateCoinsWithTradingPairs(
            TestData.CoinCreationRequests
        );

        // Assert
        result.Should().BeEquivalentTo(validationFailureResult);
        _coinsRepositoryMock.VerifyNoOtherCalls();
        _tradingPairsRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateCoinsWithTradingPairs_WhenValidRequest_InsertsCoinsAndPairsAndReturnsCreatedCoins()
    {
        // Arrange
        var requests = TestData.CoinCreationRequests;

        // Setup validation
        _coinsValidatorMock
            .Setup(validator => validator.ValidateCoinCreationRequests(requests))
            .ReturnsAsync(Result.Ok());

        // Setup repository calls needed during the process
        _coinsRepositoryMock
            .Setup(repo =>
                repo.GetCoinsBySymbolNamePairs(It.IsAny<IEnumerable<CoinSymbolNamePair>>())
            )
            .ReturnsAsync(TestData.InsertedCoinsWithIds);
        _exchangesRepositoryMock
            .Setup(repo => repo.GetAllExchanges())
            .ReturnsAsync(TestData.Exchanges);
        _coinsRepositoryMock
            .Setup(repo => repo.GetCoinsByIdsWithRelations(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(TestData.FinalCoinsWithRelations);

        // Act
        var result = await _testedService.CreateCoinsWithTradingPairs(requests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(TestData.ExpectedCreatedCoinsResult);

        // Verify repository calls with specific arguments
        _coinsRepositoryMock.Verify(
            repo => repo.InsertCoins(Its.EquivalentTo(TestData.AllNewCoinEntities)),
            Times.Once
        );
        _coinsRepositoryMock.Verify(
            repo =>
                repo.GetCoinsBySymbolNamePairs(Its.EquivalentTo(TestData.ExpectedSymbolNamePairs)),
            Times.Once
        );
        _exchangesRepositoryMock.Verify(repo => repo.GetAllExchanges(), Times.Once);
        _tradingPairsRepositoryMock.Verify(
            repo =>
                repo.InsertTradingPairs(Its.EquivalentTo(TestData.ExpectedTradingPairsToInsert)),
            Times.Once
        );
        _coinsRepositoryMock.Verify(
            repo => repo.GetCoinsByIdsWithRelations(Its.EquivalentTo(TestData.ExpectedMainCoinIds)),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCoinsMarketData_Always_CallsValidationOnce()
    {
        // Arrange
        var requests = TestData.ValidMarketDataUpdateRequests;

        _coinsValidatorMock
            .Setup(validator =>
                validator.ValidateMarketDataUpdateRequests(
                    It.IsAny<IEnumerable<CoinMarketDataUpdateRequest>>()
                )
            )
            .ReturnsAsync(Result.Ok());

        // Act
        await _testedService.UpdateCoinsMarketData(requests);

        // Assert
        _coinsValidatorMock.Verify(
            validator => validator.ValidateMarketDataUpdateRequests(requests),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenValidationFails_ReturnsFailureResult()
    {
        // Arrange
        var validationFailureResult = Result.Fail(
            new GenericErrors.BadRequestError("Market data validation failed")
        );

        _coinsValidatorMock
            .Setup(validator =>
                validator.ValidateMarketDataUpdateRequests(
                    It.IsAny<IEnumerable<CoinMarketDataUpdateRequest>>()
                )
            )
            .ReturnsAsync(validationFailureResult);

        // Act
        var result = await _testedService.UpdateCoinsMarketData(
            TestData.ValidMarketDataUpdateRequests
        );

        // Assert
        result.Should().BeEquivalentTo(validationFailureResult);

        // Verify that no repository calls are made after validation fails
        _coinsRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateCoinsMarketData_WhenValidRequest_UpdatesCoinsAndReturnsUpdatedData()
    {
        // Arrange
        var requests = TestData.ValidMarketDataUpdateRequests;
        var requestIds = requests.Select(request => request.Id);

        _coinsValidatorMock
            .Setup(validator =>
                validator.ValidateMarketDataUpdateRequests(
                    It.IsAny<IEnumerable<CoinMarketDataUpdateRequest>>()
                )
            )
            .ReturnsAsync(Result.Ok());

        _coinsRepositoryMock
            .Setup(repo => repo.GetCoinsByIds(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(TestData.ExistingCoinsForUpdate);
        _coinsRepositoryMock
            .Setup(repo => repo.UpdateCoins(It.IsAny<IEnumerable<CoinsEntity>>()))
            .ReturnsAsync(TestData.UpdatedCoinsEntitiesFromRepo);

        // Act
        var result = await _testedService.UpdateCoinsMarketData(requests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(TestData.ExpectedUpdatedCoinsResult);

        // Verify repository calls
        _coinsRepositoryMock.Verify(repo => repo.GetCoinsByIds(requestIds), Times.Once);
        _coinsRepositoryMock.Verify(
            repo => repo.UpdateCoins(Its.EquivalentTo(TestData.ExpectedCoinsToUpdateEntities)),
            Times.Once
        );
    }

    [Fact]
    public async Task ReplaceAllTradingPairs_Always_CallsValidationOnce()
    {
        // Arrange
        var requests = TestData.TradingPairReplacementRequests;

        _tradingPairsValidatorMock
            .Setup(validator =>
                validator.ValidateTradingPairsReplacementRequests(
                    It.IsAny<IEnumerable<TradingPairCreationRequest>>()
                )
            )
            .ReturnsAsync(Result.Ok());

        // Act
        await _testedService.ReplaceAllTradingPairs(requests);

        // Assert
        _tradingPairsValidatorMock.Verify(
            validator => validator.ValidateTradingPairsReplacementRequests(requests),
            Times.Once
        );
    }

    [Fact]
    public async Task ReplaceAllTradingPairs_WhenValidationFails_ReturnsFailureResult()
    {
        // Arrange
        var validationFailureResult = Result.Fail(
            new GenericErrors.BadRequestError("Validation failed")
        );

        _tradingPairsValidatorMock
            .Setup(validator =>
                validator.ValidateTradingPairsReplacementRequests(
                    It.IsAny<IEnumerable<TradingPairCreationRequest>>()
                )
            )
            .ReturnsAsync(validationFailureResult);

        // Act
        var result = await _testedService.ReplaceAllTradingPairs(
            TestData.TradingPairReplacementRequests
        );

        // Assert
        result.Should().BeEquivalentTo(validationFailureResult);

        // Verify that no repository calls are made after validation fails
        _exchangesRepositoryMock.VerifyNoOtherCalls();
        _tradingPairsRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReplaceAllTradingPairs_WhenValidRequest_ReplacesPairsAndReturnsNewPairs()
    {
        // Arrange
        _tradingPairsValidatorMock
            .Setup(validator =>
                validator.ValidateTradingPairsReplacementRequests(
                    It.IsAny<IEnumerable<TradingPairCreationRequest>>()
                )
            )
            .ReturnsAsync(Result.Ok());
        _exchangesRepositoryMock
            .Setup(repo => repo.GetAllExchanges())
            .ReturnsAsync(TestData.Exchanges);
        _tradingPairsRepositoryMock
            .Setup(repo => repo.ReplaceAllTradingPairs(It.IsAny<IEnumerable<TradingPairsEntity>>()))
            .Returns(Task.CompletedTask);
        _coinsRepositoryMock
            .Setup(repo => repo.GetAllCoinsWithRelations())
            .ReturnsAsync(TestData.CoinEntities);

        // Act
        var result = await _testedService.ReplaceAllTradingPairs(
            TestData.TradingPairReplacementRequests
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(TestData.ExpectedCoins);

        // Verify that all repository calls are made
        _exchangesRepositoryMock.Verify(repo => repo.GetAllExchanges(), Times.Once);
        _tradingPairsRepositoryMock.Verify(
            repo => repo.ReplaceAllTradingPairs(Its.EquivalentTo(TestData.NewTradingPairsEntities)),
            Times.Once
        );
        _coinsRepositoryMock.Verify(repo => repo.GetAllCoinsWithRelations(), Times.Once);
    }

    [Fact]
    public async Task DeleteCoinWithTradingPairs_WhenCoinDoesNotExist_ReturnsNotFoundFailure()
    {
        // Arrange
        const int nonExistingId = 99;

        _coinsRepositoryMock.Setup(repo => repo.CheckCoinExists(nonExistingId)).ReturnsAsync(false);

        // Act
        var result = await _testedService.DeleteCoinWithTradingPairs(nonExistingId);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeOfType<GenericErrors.NotFoundError>();

        // Verify that no other repository calls are made
        _tradingPairsRepositoryMock.VerifyNoOtherCalls();
        _coinsRepositoryMock.Verify(repo => repo.CheckCoinExists(nonExistingId), Times.Once);
        _coinsRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteCoinWithTradingPairs_WhenCoinExists_DeletesCoinAndPairsAndReturnsOk()
    {
        // Arrange
        const int existingId = 1;
        _coinsRepositoryMock.Setup(repo => repo.CheckCoinExists(existingId)).ReturnsAsync(true);

        // Act
        var result = await _testedService.DeleteCoinWithTradingPairs(existingId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify that necessary repository calls are made
        _coinsRepositoryMock.Verify(repo => repo.CheckCoinExists(existingId), Times.Once);
        _tradingPairsRepositoryMock.Verify(
            repo => repo.DeleteTradingPairsForIdCoin(existingId),
            Times.Once
        );
        _coinsRepositoryMock.Verify(repo => repo.DeleteCoinById(existingId), Times.Once);
    }

    [Fact]
    public async Task DeleteCoinsNotReferencedByTradingPairs_Always_CallsRepositoryMethod()
    {
        // Act
        await _testedService.DeleteCoinsNotReferencedByTradingPairs();

        // Assert
        _coinsRepositoryMock.Verify(
            repo => repo.DeleteCoinsNotReferencedByTradingPairs(),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteAllCoinsWithRelatedData_Always_CallsRepositoryMethod()
    {
        // Act
        await _testedService.DeleteAllCoinsWithRelatedData();

        // Assert
        _coinsRepositoryMock.Verify(repo => repo.DeleteAllCoinsWithRelations(), Times.Once);
    }

    [Fact]
    public async Task CreateQuoteCoins_Always_CallsValidationOnce()
    {
        // Arrange
        var requests = TestData.ValidQuoteCoinCreationRequests;

        _coinsValidatorMock
            .Setup(validator =>
                validator.ValidateQuoteCoinCreationRequests(
                    It.IsAny<IEnumerable<QuoteCoinCreationRequest>>()
                )
            )
            .ReturnsAsync(Result.Ok());

        // Act
        await _testedService.CreateQuoteCoins(requests);

        // Assert
        _coinsValidatorMock.Verify(
            validator => validator.ValidateQuoteCoinCreationRequests(requests),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateQuoteCoins_WhenValidationFails_ReturnsFailureResult()
    {
        // Arrange
        var validationFailureResult = Result.Fail(
            new GenericErrors.BadRequestError("Quote coin validation failed")
        );

        _coinsValidatorMock
            .Setup(validator =>
                validator.ValidateQuoteCoinCreationRequests(
                    It.IsAny<IEnumerable<QuoteCoinCreationRequest>>()
                )
            )
            .ReturnsAsync(validationFailureResult);

        // Act
        var result = await _testedService.CreateQuoteCoins(TestData.ValidQuoteCoinCreationRequests);

        // Assert
        result.Should().BeEquivalentTo(validationFailureResult);
        _coinsRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateQuoteCoins_WhenValidRequest_InsertsCoinsAndReturnsCreatedCoins()
    {
        // Arrange
        var requests = TestData.ValidQuoteCoinCreationRequests;

        // Setup validation
        _coinsValidatorMock
            .Setup(validator => validator.ValidateQuoteCoinCreationRequests(requests))
            .ReturnsAsync(Result.Ok());

        // Setup repository calls
        _coinsRepositoryMock
            .Setup(repo => repo.InsertCoins(It.IsAny<IEnumerable<CoinsEntity>>()))
            .ReturnsAsync(TestData.InsertedQuoteCoinsWithIds);

        // Act
        var result = await _testedService.CreateQuoteCoins(requests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(TestData.ExpectedCreatedQuoteCoinsResult);

        // Verify repository calls
        _coinsRepositoryMock.Verify(
            repo => repo.InsertCoins(Its.EquivalentTo(TestData.NewQuoteCoinEntitiesFromRequests)),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateQuoteCoins_WhenEmptyRequest_ReturnsEmptyResult()
    {
        // Arrange
        var emptyRequests = Enumerable.Empty<QuoteCoinCreationRequest>();

        _coinsValidatorMock
            .Setup(validator => validator.ValidateQuoteCoinCreationRequests(emptyRequests))
            .ReturnsAsync(Result.Ok());

        _coinsRepositoryMock
            .Setup(repo => repo.InsertCoins(It.IsAny<IEnumerable<CoinsEntity>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _testedService.CreateQuoteCoins(emptyRequests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateQuoteCoins_WhenSingleQuoteCoin_CreatesAndReturnsCorrectly()
    {
        // Arrange
        var singleRequest = new[] { TestData.SingleQuoteCoinCreationRequest };

        _coinsValidatorMock
            .Setup(validator => validator.ValidateQuoteCoinCreationRequests(singleRequest))
            .ReturnsAsync(Result.Ok());

        _coinsRepositoryMock
            .Setup(repo => repo.InsertCoins(It.IsAny<IEnumerable<CoinsEntity>>()))
            .ReturnsAsync([TestData.SingleInsertedQuoteCoin]);

        // Act
        var result = await _testedService.CreateQuoteCoins(singleRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo([TestData.ExpectedSingleQuoteCoinResult]);
        result.Value.Should().HaveCount(1);
    }

    private static class TestData
    {
        public static readonly IEnumerable<CoinsEntity> CoinEntities =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 101,
                        IdCoinMain = 1,
                        IdCoinQuote = 2,
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                            IsStablecoin = true,
                        },
                        Exchanges =
                        [
                            new() { Id = 1, Name = "Binance" },
                            new() { Id = 2, Name = "Bybit" },
                        ],
                    },
                ],
            },
            new()
            {
                Id = 2,
                Symbol = "USDT",
                Name = "Tether",
                IsStablecoin = true,
                TradingPairs = [],
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<Coin> ExpectedCoins =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 101,
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                        },
                        Exchanges = [Exchange.Binance, Exchange.Bybit],
                    },
                ],
            },
            new()
            {
                Id = 2,
                Symbol = "USDT",
                Name = "Tether",
                Category = CoinCategory.Stablecoin,
                TradingPairs = [],
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<int> ExistingIds = [1, 2, 3];

        public static readonly IEnumerable<int> PartiallyExistingIds = [1, 2];

        public static readonly IEnumerable<int> NonExistingIds = [98, 99];

        public static readonly IEnumerable<ExchangesEntity> Exchanges =
        [
            new() { Id = 1, Name = "Binance" },
            new() { Id = 2, Name = "Bybit" },
        ];

        public static readonly IEnumerable<CoinCreationRequest> CoinCreationRequests =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                    new()
                    {
                        CoinQuote = new() { Symbol = "DOGE", Name = "Dogecoin" },
                        Exchanges = [Exchange.Bybit],
                    },
                ],
            },
            new()
            {
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance, Exchange.Bybit],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<CoinsEntity> NewMainCoinEntitiesFromRequest =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
            new()
            {
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
            },
        ];

        public static readonly IEnumerable<CoinsEntity> NewQuoteCoinEntitiesFromRequest =
        [
            new() { Symbol = "DOGE", Name = "Dogecoin" },
        ];

        public static readonly IEnumerable<CoinsEntity> AllNewCoinEntities =
            NewMainCoinEntitiesFromRequest.Concat(NewQuoteCoinEntitiesFromRequest);

        public static readonly IEnumerable<CoinSymbolNamePair> ExpectedSymbolNamePairs =
        [
            new() { Symbol = "BTC", Name = "Bitcoin" },
            new() { Symbol = "ETH", Name = "Ethereum" },
            new() { Symbol = "DOGE", Name = "Dogecoin" },
        ];

        public static readonly IEnumerable<CoinsEntity> InsertedCoinsWithIds =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
            },
            new()
            {
                Id = 4,
                Symbol = "DOGE",
                Name = "Dogecoin",
            },
        ];

        public static readonly IEnumerable<int> ExpectedMainCoinIds = InsertedCoinsWithIds
            .Where(insertedCoin =>
                NewMainCoinEntitiesFromRequest.Any(newCoin =>
                    newCoin.Symbol == insertedCoin.Symbol && newCoin.Name == insertedCoin.Name
                )
            )
            .Select(insertedCoin => insertedCoin.Id);

        public static readonly IEnumerable<TradingPairsEntity> ExpectedTradingPairsToInsert =
        [
            new()
            {
                IdCoinMain = 1,
                IdCoinQuote = 2,
                Exchanges = [Exchanges.First(e => e.Id == 1)],
            },
            new()
            {
                IdCoinMain = 1,
                IdCoinQuote = 4,
                Exchanges = [Exchanges.First(e => e.Id == 2)],
            },
            new()
            {
                IdCoinMain = 3,
                IdCoinQuote = 2,
                Exchanges = [.. Exchanges],
            },
        ];

        public static readonly IEnumerable<CoinsEntity> FinalCoinsWithRelations =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 101,
                        IdCoinMain = 1,
                        IdCoinQuote = 2,
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                            IsStablecoin = true,
                        },
                        Exchanges = [Exchanges.First(e => e.Id == 1)],
                    },
                    new()
                    {
                        Id = 102,
                        IdCoinMain = 1,
                        IdCoinQuote = 4,
                        CoinQuote = new()
                        {
                            Id = 4,
                            Symbol = "DOGE",
                            Name = "Dogecoin",
                        },
                        Exchanges = [Exchanges.First(e => e.Id == 2)],
                    },
                ],
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
                TradingPairs =
                [
                    new()
                    {
                        Id = 103,
                        IdCoinMain = 3,
                        IdCoinQuote = 2,
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                            IsStablecoin = true,
                        },
                        Exchanges = [.. Exchanges],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<Coin> ExpectedCreatedCoinsResult =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                TradingPairs =
                [
                    new()
                    {
                        Id = 101,
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                        },
                        Exchanges = [Exchange.Binance],
                    },
                    new()
                    {
                        Id = 102,
                        CoinQuote = new()
                        {
                            Id = 4,
                            Symbol = "DOGE",
                            Name = "Dogecoin",
                        },
                        Exchanges = [Exchange.Bybit],
                    },
                ],
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
                TradingPairs =
                [
                    new()
                    {
                        Id = 103,
                        CoinQuote = new()
                        {
                            Id = 2,
                            Symbol = "USDT",
                            Name = "Tether",
                            Category = CoinCategory.Stablecoin,
                        },
                        Exchanges = [Exchange.Binance, Exchange.Bybit],
                    },
                ],
            },
        ];

        public static readonly IEnumerable<TradingPairCreationRequest> TradingPairReplacementRequests =
        [
            new()
            {
                IdCoinMain = 1,
                IdCoinQuote = 2,
                Exchanges = [Exchange.Binance],
            }, // BTC/USDT on Binance
            new()
            {
                IdCoinMain = 3,
                IdCoinQuote = 2,
                Exchanges = [Exchange.Bybit],
            }, // ETH/USDT on Bybit
        ];

        public static readonly IEnumerable<TradingPairsEntity> NewTradingPairsEntities =
        [
            new()
            {
                IdCoinMain = 1,
                IdCoinQuote = 2,
                Exchanges = [Exchanges.First(exchange => exchange.Id == (int)Exchange.Binance)],
            },
            new()
            {
                IdCoinMain = 3,
                IdCoinQuote = 2,
                Exchanges = [Exchanges.First(exchange => exchange.Id == (int)Exchange.Bybit)],
            },
        ];

        public static readonly IEnumerable<CoinMarketDataUpdateRequest> ValidMarketDataUpdateRequests =
        [
            new()
            {
                Id = 1,
                MarketCapUsd = 1_000_000_000,
                PriceUsd = 50000m,
                PriceChangePercentage24h = 1.5m,
            }, // Update BTC
            new()
            {
                Id = 3,
                MarketCapUsd = 500_000_000,
                PriceUsd = 4000m,
                PriceChangePercentage24h = -0.5m,
            }, // Update ETH
        ];

        // Simulate coins fetched from the repository before update
        public static readonly IEnumerable<CoinsEntity> ExistingCoinsForUpdate =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
            },
        ];

        // Expected entities passed to the UpdateCoins repository method
        public static readonly IEnumerable<CoinsEntity> ExpectedCoinsToUpdateEntities =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1_000_000_000,
                PriceUsd = 50000m.ToString(),
                PriceChangePercentage24h = 1.5m,
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
                MarketCapUsd = 500_000_000,
                PriceUsd = 4000m.ToString(),
                PriceChangePercentage24h = -0.5m,
            },
        ];

        // Simulate the updated coins returned by the repository
        public static readonly IEnumerable<CoinsEntity> UpdatedCoinsEntitiesFromRepo =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1_000_000_000,
                PriceUsd = 50000m.ToString(),
                PriceChangePercentage24h = 1.5m,
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
                MarketCapUsd = 500_000_000,
                PriceUsd = 4000m.ToString(),
                PriceChangePercentage24h = -0.5m,
            },
        ];

        // Expected final result from the service method (mapped to Coin DTO)
        public static readonly IEnumerable<Coin> ExpectedUpdatedCoinsResult =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                MarketCapUsd = 1_000_000_000,
                PriceUsd = 50000m.ToString(),
                PriceChangePercentage24h = 1.5m,
                TradingPairs = [],
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
                MarketCapUsd = 500_000_000,
                PriceUsd = 4000m.ToString(),
                PriceChangePercentage24h = -0.5m,
                TradingPairs = [],
            },
        ];

        public static readonly IEnumerable<QuoteCoinCreationRequest> ValidQuoteCoinCreationRequests =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
            new()
            {
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
            },
        ];

        public static readonly IEnumerable<CoinsEntity> InsertedQuoteCoinsWithIds =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
            },
        ];

        public static readonly IEnumerable<TradingPairCoinQuote> ExpectedCreatedQuoteCoinsResult =
        [
            new()
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
            },
            new()
            {
                Id = 3,
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
            },
        ];

        public static readonly QuoteCoinCreationRequest SingleQuoteCoinCreationRequest = new()
        {
            Symbol = "BTC",
            Name = "Bitcoin",
            IdCoinGecko = "bitcoin",
        };

        public static readonly CoinsEntity SingleInsertedQuoteCoin = new()
        {
            Id = 1,
            Symbol = "BTC",
            Name = "Bitcoin",
            IdCoinGecko = "bitcoin",
        };

        public static readonly TradingPairCoinQuote ExpectedSingleQuoteCoinResult = new()
        {
            Id = 1,
            Symbol = "BTC",
            Name = "Bitcoin",
            IdCoinGecko = "bitcoin",
        };

        public static readonly IEnumerable<CoinsEntity> NewQuoteCoinEntitiesFromRequests =
        [
            new()
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                IdCoinGecko = "bitcoin",
                IsFiat = false,
                IsStablecoin = false,
            },
            new()
            {
                Symbol = "ETH",
                Name = "Ethereum",
                IdCoinGecko = "ethereum",
                IsFiat = false,
                IsStablecoin = false,
            },
        ];
    }
}
