using Microsoft.EntityFrameworkCore;
using SVC_Kline.ApiContracts.Requests;
using SVC_Kline.ApiContracts.Responses;
using SVC_Kline.Domain.Entities;
using SVC_Kline.Infrastructure;

namespace SVC_Kline.Repositories;

/// <summary>
/// Repository for handling operations related to Kline data using Entity Framework.
/// </summary>
public class KlineDataRepository(KlineDataDbContext context) : IKlineDataRepository
{
    private readonly KlineDataDbContext _context = context;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, IEnumerable<KlineData>>> GetAllKlineData()
    {
        var klineDataEntities = await _context.KlineData.ToListAsync();
        return klineDataEntities
            .Select(Mapping.ToKlineData)
            .GroupBy(k => k.IdTradingPair)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
    }

    /// <inheritdoc />
    public async Task InsertKlineData(IEnumerable<KlineDataCreationRequest> klineDataList)
    {
        var klineDataEntities = klineDataList.Select(Mapping.ToKlineDataEntity);
        await _context.KlineData.AddRangeAsync(klineDataEntities);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task ReplaceAllKlineData(KlineDataCreationRequest[] newKlineData)
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
        public static KlineDataEntity ToKlineDataEntity(KlineDataCreationRequest klineDataNew) =>
            new()
            {
                IdTradingPair = klineDataNew.IdTradingPair,
                OpenTime = klineDataNew.OpenTime,
                OpenPrice = klineDataNew.OpenPrice.ToString(),
                HighPrice = klineDataNew.HighPrice.ToString(),
                LowPrice = klineDataNew.LowPrice.ToString(),
                ClosePrice = klineDataNew.ClosePrice.ToString(),
                Volume = klineDataNew.Volume.ToString(),
                CloseTime = klineDataNew.CloseTime,
            };

        public static KlineData ToKlineData(KlineDataEntity klineDataEntity) =>
            new()
            {
                IdTradingPair = klineDataEntity.IdTradingPair,
                OpenTime = klineDataEntity.OpenTime,
                OpenPrice = decimal.Parse(klineDataEntity.OpenPrice),
                HighPrice = decimal.Parse(klineDataEntity.HighPrice),
                LowPrice = decimal.Parse(klineDataEntity.LowPrice),
                ClosePrice = decimal.Parse(klineDataEntity.ClosePrice),
                Volume = decimal.Parse(klineDataEntity.Volume),
                CloseTime = klineDataEntity.CloseTime,
            };
    }
}
