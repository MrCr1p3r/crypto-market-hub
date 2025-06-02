using System.Data;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using SVC_Coins.Domain.Entities;
using SVC_Coins.Domain.ValueObjects;
using SVC_Coins.Infrastructure;
using SVC_Coins.Repositories.Interfaces;

namespace SVC_Coins.Repositories;

/// <summary>
/// Repository for managing trading pairs in the database.
/// </summary>
public class TradingPairsRepository(CoinsDbContext context) : ITradingPairsRepository
{
    private readonly CoinsDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<TradingPairsEntity>> GetTradingPairsByCoinIdPairs(
        IEnumerable<TradingPairCoinIdsPair> pairs
    )
    {
        var tradingPairs = pairs.Select(Mapping.ToTradingPairsEntity).ToList();

        // Add UseTempDB if needed
        var bulkConfig = new BulkConfig
        {
            UpdateByProperties =
            [
                nameof(TradingPairsEntity.IdCoinMain),
                nameof(TradingPairsEntity.IdCoinQuote),
            ],
        };

        await _context.BulkReadAsync(tradingPairs, bulkConfig);

        var foundTradingPairs = tradingPairs.Where(tp => tp.Id != 0).ToList();
        return foundTradingPairs;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TradingPairsEntity>> InsertTradingPairs(
        IEnumerable<TradingPairsEntity> tradingPairs
    )
    {
        await _context.TradingPairs.AddRangeAsync(tradingPairs);
        await _context.SaveChangesAsync();

        return tradingPairs;
    }

    /// <inheritdoc />
    public async Task ReplaceAllTradingPairs(IEnumerable<TradingPairsEntity> tradingPairs)
    {
        _context.TradingPairs.RemoveRange(_context.TradingPairs);
        await _context.TradingPairs.AddRangeAsync(tradingPairs);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteMainCoinTradingPairs(int idCoin) =>
        await _context.TradingPairs.Where(tp => tp.IdCoinMain == idCoin).ExecuteDeleteAsync();

    private static class Mapping
    {
        public static TradingPairsEntity ToTradingPairsEntity(TradingPairCoinIdsPair pair) =>
            new() { IdCoinMain = pair.IdCoinMain, IdCoinQuote = pair.IdCoinQuote };
    }
}
