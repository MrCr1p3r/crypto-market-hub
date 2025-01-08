using FluentResults;
using Microsoft.EntityFrameworkCore;
using SVC_Coins.Models.Entities;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;
using SVC_Coins.Repositories.Interfaces;

namespace SVC_Coins.Repositories;

/// <summary>
/// Repository for handling operations related to coins using Entity Framework.
/// </summary>
/// <param name="context">The DbContext used for database operations.</param>
public class CoinsRepository(CoinsDbContext context) : ICoinsRepository
{
    private readonly CoinsDbContext _context = context;

    /// <inheritdoc />
    public async Task<Result> InsertCoin(CoinNew coin)
    {
        var coinEntity = Mapping.ToCoinEntity(coin);
        if (await CheckCoinExists(coinEntity))
            return Result.Fail("Coin already exists in the database.");

        await _context.Coins.AddAsync(coinEntity);
        await _context.SaveChangesAsync();
        return Result.Ok();
    }

    private async Task<bool> CheckCoinExists(CoinsEntity coin) =>
        await _context.Coins.AnyAsync(c => c.Name == coin.Name && c.Symbol == coin.Symbol);

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetAllCoins()
    {
        var coinsWithTradingPairs = await _context.Coins.Include(c => c.TradingPairs).ToListAsync();

        return coinsWithTradingPairs.Select(Mapping.ToCoin);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteCoin(int idCoin)
    {
        var coinToDelete = _context.Coins.Where(coin => coin.Id == idCoin);
        if (!coinToDelete.Any())
            return Result.Fail($"Coin with ID {idCoin} not found.");

        var tradingPairsToDelete = _context.TradingPairs.Where(tp =>
            tp.IdCoinMain == idCoin || tp.IdCoinQuote == idCoin
        );
        _context.TradingPairs.RemoveRange(tradingPairsToDelete);

        _context.Coins.RemoveRange(coinToDelete);

        await _context.SaveChangesAsync();
        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result<int>> InsertTradingPair(TradingPairNew tradingPair)
    {
        var tradingPairEntity = Mapping.ToTradingPairEntity(tradingPair);

        if (!await CheckCoinsExist(tradingPairEntity))
            return Result.Fail("One or both coins do not exist in the Coins table.");

        if (await CheckTradingPairExists(tradingPairEntity))
            return Result.Fail("This trading pair already exists.");

        await _context.TradingPairs.AddAsync(tradingPairEntity);
        await _context.SaveChangesAsync();

        return Result.Ok(tradingPairEntity.Id);
    }

    private async Task<bool> CheckCoinsExist(TradingPairsEntity tradingPairEntity) =>
        await _context.Coins.AnyAsync(coin => coin.Id == tradingPairEntity.IdCoinMain)
        && await _context.Coins.AnyAsync(coin => coin.Id == tradingPairEntity.IdCoinQuote);

    private async Task<bool> CheckTradingPairExists(TradingPairsEntity tradingPairEntity) =>
        await _context.TradingPairs.AnyAsync(tp =>
            tp.IdCoinMain == tradingPairEntity.IdCoinMain
            && tp.IdCoinQuote == tradingPairEntity.IdCoinQuote
        );

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetQuoteCoinsPrioritized()
    {
        var prioritizedCoins = await _context
            .Coins.Where(c => c.QuoteCoinPriority != null)
            .OrderBy(c => c.QuoteCoinPriority)
            .ToListAsync();

        return prioritizedCoins.Select(Mapping.ToCoin);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetCoinsByIds(IEnumerable<int> ids)
    {
        var coins = await _context
            .Coins.Where(coin => ids.Contains(coin.Id))
            .Include(c => c.TradingPairs)
            .ThenInclude(tp => tp.CoinQuote)
            .ToListAsync();

        return coins.Select(Mapping.ToCoin);
    }

    private static class Mapping
    {
        public static CoinsEntity ToCoinEntity(CoinNew coinNew) =>
            new() { Name = coinNew.Name, Symbol = coinNew.Symbol };

        public static Coin ToCoin(CoinsEntity coinEntity) =>
            new()
            {
                Id = coinEntity.Id,
                Name = coinEntity.Name,
                Symbol = coinEntity.Symbol,
                TradingPairs = coinEntity.TradingPairs.Select(ToTradingPair),
            };

        public static TradingPair ToTradingPair(TradingPairsEntity tradingPairEntity) =>
            new()
            {
                Id = tradingPairEntity.Id,
                CoinQuote = ToTradingPairCoin(tradingPairEntity.CoinQuote),
            };

        public static TradingPairCoin ToTradingPairCoin(CoinsEntity coinEntity) =>
            new()
            {
                Id = coinEntity.Id,
                Name = coinEntity.Name,
                Symbol = coinEntity.Symbol,
            };

        public static TradingPairsEntity ToTradingPairEntity(TradingPairNew tradingPair) =>
            new() { IdCoinMain = tradingPair.IdCoinMain, IdCoinQuote = tradingPair.IdCoinQuote };
    }
}
