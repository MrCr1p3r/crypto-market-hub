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
    public async Task InsertCoin(CoinNew coin)
    {
        var coinEntity = Mapping.ToCoinEntity(coin);
        await _context.Coins.AddAsync(coinEntity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetAllCoins()
    {
        var coinsWithTradingPairs = await _context.Coins
            .Include(c => c.TradingPairs)
            .ToListAsync();

        var coins = coinsWithTradingPairs.Select(Mapping.ToCoin);
        return coins;
    }

    /// <inheritdoc />
    public async Task DeleteCoin(int idCoin)
    {
        var tradingPairsToDelete = _context.TradingPairs
            .Where(tp => tp.IdCoinMain == idCoin || tp.IdCoinQuote == idCoin);
        _context.TradingPairs.RemoveRange(tradingPairsToDelete);

        var coinToDelete = _context.Coins.Where(coin => coin.Id == idCoin);
        _context.Coins.RemoveRange(coinToDelete);

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> InsertTradingPair(TradingPairNew tradingPair)
    {
        var tradingPairEntity = Mapping.ToTradingPairEntity(tradingPair);
        await _context.TradingPairs.AddAsync(tradingPairEntity);
        await _context.SaveChangesAsync();
        return tradingPairEntity.Id;
    }

    private static class Mapping
    {
        public static CoinEntity ToCoinEntity(CoinNew coinNew) => new()
        {
            Name = coinNew.Name,
            Symbol = coinNew.Symbol
        };

        public static Coin ToCoin(CoinEntity coinEntity) => new()
        {
            Id = coinEntity.Id,
            Name = coinEntity.Name,
            Symbol = coinEntity.Symbol,
            TradingPairs = coinEntity.TradingPairs.Select(ToTradingPair).ToList()
        };

        public static TradingPair ToTradingPair(TradingPairEntity tradingPairEntity) => new()
        {
            Id = tradingPairEntity.Id,
            CoinQuote = ToTradingPairCoin(tradingPairEntity.CoinQuote)
        };

        public static TradingPairCoin ToTradingPairCoin(CoinEntity coinEntity) => new()
        {
            Id = coinEntity.Id,
            Name = coinEntity.Name,
            Symbol = coinEntity.Symbol
        };

        public static TradingPairEntity ToTradingPairEntity(TradingPairNew tradingPair) => new()
        {
            IdCoinMain = tradingPair.IdCoinMain,
            IdCoinQuote = tradingPair.IdCoinQuote,
        };
    }
}
