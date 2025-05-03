using System.Globalization;
using System.Transactions;
using FluentResults;
using SharedLibrary.Enums;
using SharedLibrary.Errors;
using SVC_Coins.ApiModels.Requests;
using SVC_Coins.ApiModels.Requests.CoinCreation;
using SVC_Coins.ApiModels.Responses;
using SVC_Coins.Domain.Entities;
using SVC_Coins.Domain.ValueObjects;
using SVC_Coins.Repositories.Interfaces;
using SVC_Coins.Services.Interfaces;
using SVC_Coins.Services.Validators.Interfaces;

namespace SVC_Coins.Services;

/// <summary>
/// Service for handling coins-related business operations.
/// </summary>
public partial class CoinsService(
    ICoinsRepository coinsRepository,
    ITradingPairsRepository tradingPairsRepository,
    IExchangesRepository exchangesRepository,
    ICoinsValidator coinsValidator,
    ITradingPairsValidator tradingPairsValidator
) : ICoinsService
{
    private readonly ICoinsRepository _coinsRepository = coinsRepository;
    private readonly ITradingPairsRepository _tradingPairsRepository = tradingPairsRepository;
    private readonly IExchangesRepository _exchangesRepository = exchangesRepository;
    private readonly ICoinsValidator _coinsValidator = coinsValidator;
    private readonly ITradingPairsValidator _tradingPairsValidator = tradingPairsValidator;

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetAllCoins()
    {
        var coins = await _coinsRepository.GetAllCoinsWithRelations();
        return coins.Select(Mapping.ToCoin);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetCoinsByIds(IEnumerable<int> ids)
    {
        var coins = await _coinsRepository.GetCoinsByIdsWithRelations(ids);
        return coins.Select(Mapping.ToCoin);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> CreateCoinsWithTradingPairs(
        IEnumerable<CoinCreationRequest> requests
    )
    {
        var validationResult = await _coinsValidator.ValidateCoinCreationRequests(requests);
        if (validationResult.IsFailed)
        {
            return Result.Fail(validationResult.Errors);
        }

        var insertedMainCoinIds = await InsertCoinsWithTradingPairs(requests);
        var insertedCoinsWithRelations = await GetInsertedCoinsWithRelations(insertedMainCoinIds);

        return Result.Ok(insertedCoinsWithRelations.Select(Mapping.ToCoin));
    }

    private async Task<IEnumerable<int>> InsertCoinsWithTradingPairs(
        IEnumerable<CoinCreationRequest> requests
    )
    {
        using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var insertedCoins = await InsertCoins(requests);
        await InsertTradingPairs(requests, insertedCoins);

        transaction.Complete();

        var insertedMainCoinIds = Mapping.ToMainCoinIds(insertedCoins, requests);
        return insertedMainCoinIds;
    }

    private async Task<IEnumerable<CoinsEntity>> InsertCoins(
        IEnumerable<CoinCreationRequest> requests
    )
    {
        var newCoins = Mapping.ToNewCoins(requests);
        await _coinsRepository.InsertCoins(newCoins);

        // TransactionScope has issues with entity's properties population,
        // so I query inserted coins separately afterwards.
        var symbolNamePairs = newCoins.Select(Mapping.ToSymbolNamePair);
        var insertedCoins = await _coinsRepository.GetCoinsBySymbolNamePairs(symbolNamePairs);
        return insertedCoins;
    }

    private async Task InsertTradingPairs(
        IEnumerable<CoinCreationRequest> requests,
        IEnumerable<CoinsEntity> insertedCoins
    )
    {
        var newTradingPairs = await GetNewTradingPairs(requests, insertedCoins);
        await _tradingPairsRepository.InsertTradingPairs(newTradingPairs);
    }

    private async Task<IEnumerable<TradingPairsEntity>> GetNewTradingPairs(
        IEnumerable<CoinCreationRequest> requests,
        IEnumerable<CoinsEntity> insertedCoins
    )
    {
        var exchanges = await _exchangesRepository.GetAllExchanges();
        var tradingPairs = Mapping.ToTradingPairsEntities(requests, insertedCoins, exchanges);

        return tradingPairs;
    }

    private async Task<IEnumerable<CoinsEntity>> GetInsertedCoinsWithRelations(
        IEnumerable<int> insertedCoinIds
    )
    {
        var insertedCoinsWithRelations = await _coinsRepository.GetCoinsByIdsWithRelations(
            insertedCoinIds
        );
        return insertedCoinsWithRelations;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> UpdateCoinsMarketData(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    )
    {
        var validationResult = await _coinsValidator.ValidateMarketDataUpdateRequests(requests);
        if (validationResult.IsFailed)
        {
            return Result.Fail(validationResult.Errors);
        }

        var updatedCoins = await UpdateCoins(requests);
        return Result.Ok(updatedCoins.Select(Mapping.ToCoin));
    }

    private async Task<IEnumerable<CoinsEntity>> UpdateCoins(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    )
    {
        var coinsWithUpdatedData = await GetCoinsWithUpdatedData(requests);
        var updatedCoins = await _coinsRepository.UpdateCoins(coinsWithUpdatedData);
        return updatedCoins;
    }

    private async Task<IEnumerable<CoinsEntity>> GetCoinsWithUpdatedData(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    )
    {
        var requestsById = requests.ToDictionary(request => request.Id);
        var coinsToUpdate = await _coinsRepository.GetCoinsByIds(requestsById.Keys);

        var coinsWithUpdatedData = coinsToUpdate.Select(coin =>
            Mapping.ToCoinWithUpdatedData(coin, requestsById[coin.Id])
        );
        return coinsWithUpdatedData;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<TradingPair>>> ReplaceAllTradingPairs(
        IEnumerable<TradingPairCreationRequest> requests
    )
    {
        var validationResult = await _tradingPairsValidator.ValidateTradingPairsReplacementRequests(
            requests
        );
        if (validationResult.IsFailed)
        {
            return Result.Fail(validationResult.Errors);
        }

        // Does not load coin data. Change later if needed.
        var newTradingPairs = await ReplaceTradingPairs(requests);

        return Result.Ok(newTradingPairs.Select(Mapping.ToTradingPair));
    }

    private async Task<IEnumerable<TradingPairsEntity>> ReplaceTradingPairs(
        IEnumerable<TradingPairCreationRequest> requests
    )
    {
        var tradingPairsForInsertion = await GetTradingPairsForInsertion(requests);
        var newTradingPairs = await _tradingPairsRepository.ReplaceAllTradingPairs(
            tradingPairsForInsertion
        );
        return newTradingPairs;
    }

    private async Task<IEnumerable<TradingPairsEntity>> GetTradingPairsForInsertion(
        IEnumerable<TradingPairCreationRequest> requests
    )
    {
        var exchanges = await _exchangesRepository.GetAllExchanges();
        var tradingPairsForInsertion = requests.Select(request =>
            Mapping.ToTradingPairEntity(request, exchanges)
        );
        return tradingPairsForInsertion;
    }

    /// <inheritdoc />
    public async Task<Result> DeleteCoinWithTradingPairs(int idCoin)
    {
        var coinExists = await _coinsRepository.CheckCoinExists(idCoin);
        if (!coinExists)
        {
            return Result.Fail(
                new GenericErrors.NotFoundError($"Coin with ID {idCoin} not found.")
            );
        }

        using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        await _tradingPairsRepository.DeleteTradingPairsForIdCoin(idCoin);
        await _coinsRepository.DeleteCoinById(idCoin);

        transaction.Complete();

        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task DeleteAllCoinsWithRelatedData() =>
        await _coinsRepository.DeleteAllCoinsWithRelations();

    private static class Mapping
    {
        public static Coin ToCoin(CoinsEntity coinEntity) =>
            new()
            {
                Id = coinEntity.Id,
                Symbol = coinEntity.Symbol,
                Name = coinEntity.Name,
                Category = ToCoinCategory(coinEntity),
                IdCoinGecko = coinEntity.IdCoinGecko,
                MarketCapUsd = coinEntity.MarketCapUsd,
                PriceUsd = coinEntity.PriceUsd,
                PriceChangePercentage24h = coinEntity.PriceChangePercentage24h,
                TradingPairs = coinEntity.TradingPairs.Select(ToTradingPair),
            };

        private static CoinCategory? ToCoinCategory(CoinsEntity coinEntity) =>
            coinEntity switch
            {
                { IsFiat: true } => CoinCategory.Fiat,
                { IsStablecoin: true } => CoinCategory.Stablecoin,
                _ => null,
            };

        public static IEnumerable<int> ToMainCoinIds(
            IEnumerable<CoinsEntity> insertedCoins,
            IEnumerable<CoinCreationRequest> requests
        )
        {
            var insertedCoinIdByPairMap = insertedCoins.ToDictionary(
                coin => (coin.Symbol, coin.Name),
                coin => coin.Id
            );
            var requestPairs = requests
                .Select(request => (request.Symbol, request.Name))
                .ToHashSet();
            var insertedMainCoinIds = insertedCoinIdByPairMap
                .Where(kvp => requestPairs.Contains(kvp.Key))
                .Select(kvp => kvp.Value);

            return insertedMainCoinIds;
        }

        public static TradingPair ToTradingPair(TradingPairsEntity tradingPairEntity) =>
            new()
            {
                Id = tradingPairEntity.Id,
                CoinQuote = ToTradingPairCoinQuote(tradingPairEntity.CoinQuote),
                Exchanges = tradingPairEntity.Exchanges.Select(exchange => (Exchange)exchange.Id),
            };

        public static IEnumerable<CoinsEntity> ToNewCoins(IEnumerable<CoinCreationRequest> requests)
        {
            var mainCoins = requests.Select(ToCoinEntity);

            var quoteCoins = requests
                .SelectMany(request => request.TradingPairs)
                .Select(tradingPair => tradingPair.CoinQuote)
                .Where(quoteCoin => quoteCoin.Id is null)
                .Select(ToCoinEntity);

            var newCoins = mainCoins
                .Concat(quoteCoins)
                .DistinctBy(coin => (coin.Symbol, coin.Name));

            return newCoins;
        }

        private static CoinsEntity ToCoinEntity(CoinCreationRequest coinNew) =>
            new()
            {
                Symbol = coinNew.Symbol,
                Name = coinNew.Name,
                IdCoinGecko = coinNew.IdCoinGecko,
                IsFiat = coinNew.Category == CoinCategory.Fiat,
                IsStablecoin = coinNew.Category == CoinCategory.Stablecoin,
            };

        private static CoinsEntity ToCoinEntity(CoinCreationCoinQuote coinNew) =>
            new()
            {
                Symbol = coinNew.Symbol,
                Name = coinNew.Name,
                IdCoinGecko = coinNew.IdCoinGecko,
                IsFiat = coinNew.Category == CoinCategory.Fiat,
                IsStablecoin = coinNew.Category == CoinCategory.Stablecoin,
            };

        public static CoinSymbolNamePair ToSymbolNamePair(CoinsEntity coinEntity) =>
            new() { Symbol = coinEntity.Symbol, Name = coinEntity.Name };

        public static TradingPairCoinQuote ToTradingPairCoinQuote(CoinsEntity coinEntity) =>
            new()
            {
                Id = coinEntity.Id,
                Symbol = coinEntity.Symbol,
                Name = coinEntity.Name,
                Category = ToCoinCategory(coinEntity),
                IdCoinGecko = coinEntity.IdCoinGecko,
                MarketCapUsd = coinEntity.MarketCapUsd,
                PriceUsd = coinEntity.PriceUsd,
                PriceChangePercentage24h = coinEntity.PriceChangePercentage24h,
            };

        public static TradingPairsEntity ToTradingPairEntity(
            TradingPairCreationRequest tradingPair,
            IEnumerable<ExchangesEntity> exchanges
        ) =>
            new()
            {
                IdCoinMain = tradingPair.IdCoinMain,
                IdCoinQuote = tradingPair.IdCoinQuote,
                Exchanges =
                [
                    .. tradingPair.Exchanges.Select(exchange =>
                        exchanges.First(e => e.Id == (int)exchange)
                    ),
                ],
            };

        public static IEnumerable<TradingPairsEntity> ToTradingPairsEntities(
            IEnumerable<CoinCreationRequest> requests,
            IEnumerable<CoinsEntity> insertedCoins,
            IEnumerable<ExchangesEntity> exchanges
        )
        {
            var symbolNameToCoinIdMap = insertedCoins.ToDictionary(
                coin => (coin.Symbol, coin.Name),
                coin => coin.Id
            );

            return requests.SelectMany(request =>
                request.TradingPairs.Select(tradingPair =>
                    ToTradingPairEntity(request, tradingPair, symbolNameToCoinIdMap, exchanges)
                )
            );
        }

        private static TradingPairsEntity ToTradingPairEntity(
            CoinCreationRequest request,
            CoinCreationTradingPair tradingPair,
            Dictionary<(string Symbol, string Name), int> symbolNameToCoinIdMap,
            IEnumerable<ExchangesEntity> exchangeEntities
        ) =>
            new()
            {
                IdCoinMain = symbolNameToCoinIdMap[(request.Symbol, request.Name)],
                IdCoinQuote =
                    tradingPair.CoinQuote.Id
                    ?? symbolNameToCoinIdMap[
                        (tradingPair.CoinQuote.Symbol, tradingPair.CoinQuote.Name)
                    ],
                Exchanges =
                [
                    .. tradingPair.Exchanges.Select(exchange =>
                        exchangeEntities.First(entity => entity.Id == (int)exchange)
                    ),
                ],
            };

        public static CoinsEntity ToCoinWithUpdatedData(
            CoinsEntity coinEntity,
            CoinMarketDataUpdateRequest request
        )
        {
            coinEntity.MarketCapUsd = request.MarketCapUsd;
            coinEntity.PriceUsd = request.PriceUsd?.ToString(CultureInfo.InvariantCulture);
            coinEntity.PriceChangePercentage24h = request.PriceChangePercentage24h;
            return coinEntity;
        }
    }
}
