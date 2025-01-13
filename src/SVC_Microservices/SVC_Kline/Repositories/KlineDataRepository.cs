using Microsoft.EntityFrameworkCore;
using SVC_Kline.Models.Entities;
using SVC_Kline.Models.Input;
using SVC_Kline.Models.Output;
using SVC_Kline.Repositories.Interfaces;

namespace SVC_Kline.Repositories;

/// <summary>
/// Repository for handling operations related to Kline data using Entity Framework.
/// </summary>
public class KlineDataRepository(KlineDataDbContext context) : IKlineDataRepository
{
    private readonly KlineDataDbContext _context = context;

    /// <inheritdoc />
    public async Task InsertKlineData(KlineDataNew klineData)
    {
        var klineDataEntity = Mapping.ToKlineDataEntity(klineData);
        await _context.KlineData.AddAsync(klineDataEntity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task InsertManyKlineData(IEnumerable<KlineDataNew> klineDataList)
    {
        var klineDataEntities = klineDataList.Select(Mapping.ToKlineDataEntity);
        await _context.KlineData.AddRangeAsync(klineDataEntities);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, IEnumerable<KlineData>>> GetAllKlineData()
    {
        var klineDataEntities = await _context.KlineData.ToListAsync();
        return klineDataEntities
            .Select(Mapping.ToKlineData)
            .GroupBy(k => k.IdTradePair)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }

    /// <inheritdoc />
    public async Task DeleteKlineDataForTradingPair(int idTradePair)
    {
        var klineDataEntities = _context.KlineData.Where(k => k.IdTradePair == idTradePair);
        _context.KlineData.RemoveRange(klineDataEntities);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task ReplaceAllKlineData(KlineDataNew[] newKlineData)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        await _context.KlineData.ExecuteDeleteAsync();

        var newKlineDataEntities = newKlineData.Select(Mapping.ToKlineDataEntity);
        await _context.KlineData.AddRangeAsync(newKlineDataEntities);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static class Mapping
    {
        public static KlineDataEntity ToKlineDataEntity(KlineDataNew klineDataNew) =>
            new()
            {
                IdTradePair = klineDataNew.IdTradePair,
                OpenTime = klineDataNew.OpenTime,
                OpenPrice = klineDataNew.OpenPrice,
                HighPrice = klineDataNew.HighPrice,
                LowPrice = klineDataNew.LowPrice,
                ClosePrice = klineDataNew.ClosePrice,
                Volume = klineDataNew.Volume,
                CloseTime = klineDataNew.CloseTime,
            };

        public static KlineData ToKlineData(KlineDataEntity klineDataEntity) =>
            new()
            {
                IdTradePair = klineDataEntity.IdTradePair,
                OpenTime = klineDataEntity.OpenTime,
                OpenPrice = klineDataEntity.OpenPrice,
                HighPrice = klineDataEntity.HighPrice,
                LowPrice = klineDataEntity.LowPrice,
                ClosePrice = klineDataEntity.ClosePrice,
                Volume = klineDataEntity.Volume,
                CloseTime = klineDataEntity.CloseTime,
            };
    }
}
