using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SVC_Coins.Models.Entities;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;
using SVC_Coins.Repositories.Interfaces;

namespace SVC_Coins.Repositories;

/// <summary>
/// Repository for handling operations related to coins using Entity Framework and AutoMapper.
/// </summary>
/// <param name="context">The DbContext used for database operations.</param>
/// <param name="mapper">The AutoMapper instance used for mapping models.</param>
public class CoinsRepository(CoinsDbContext context, IMapper mapper) : ICoinsRepository
{
    private readonly CoinsDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    /// <inheritdoc />
    public async Task InsertCoin(CoinNew klineData)
    {
        var coinEntity = _mapper.Map<CoinEntity>(klineData);
        await _context.Coins.AddAsync(coinEntity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetAllCoins()
    {
        var coinsWithTradingPairs = await _context.Coins
            .Include(c => c.TradingPairs)
            .ToListAsync();

        var coins = _mapper.Map<IEnumerable<Coin>>(coinsWithTradingPairs);

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
}
