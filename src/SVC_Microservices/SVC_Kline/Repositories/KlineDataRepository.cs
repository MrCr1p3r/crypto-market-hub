using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;
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
    public async Task<IEnumerable<KlineDataResponse>> GetAllKlineData()
    {
        var klineDataEntities = await _context.KlineData.ToListAsync();
        return Mapping.ToKlineDataResponse(klineDataEntities);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<KlineDataResponse>> InsertKlineData(
        IEnumerable<KlineDataCreationRequest> klineDataList
    )
    {
        var klineDataEntities = klineDataList.Select(Mapping.ToKlineDataEntity);
        await _context.KlineData.AddRangeAsync(klineDataEntities);
        await _context.SaveChangesAsync();

        var insertedEntities = await GetInsertedEntities(klineDataList);

        return Mapping.ToKlineDataResponse(insertedEntities);
    }

    private async Task<List<KlineDataEntity>> GetInsertedEntities(
        IEnumerable<KlineDataCreationRequest> klineDataList
    )
    {
        var tradingPairIds = klineDataList.Select(request => request.IdTradingPair).Distinct();
        var insertedEntities = await _context
            .KlineData.Where(entity => tradingPairIds.Contains(entity.IdTradingPair))
            .ToListAsync();
        return insertedEntities;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<KlineDataResponse>> ReplaceAllKlineData(
        KlineDataCreationRequest[] newKlineData
    )
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        await _context.KlineData.ExecuteDeleteAsync();

        var newKlineDataEntities = newKlineData.Select(Mapping.ToKlineDataEntity);
        await _context.KlineData.AddRangeAsync(newKlineDataEntities);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Get the freshly inserted data from the database
        var insertedEntities = await _context.KlineData.ToListAsync();
        return Mapping.ToKlineDataResponse(insertedEntities);
    }

    private static class Mapping
    {
        public static IEnumerable<KlineDataResponse> ToKlineDataResponse(
            IEnumerable<KlineDataEntity> insertedEntities
        ) =>
            insertedEntities
                .GroupBy(entity => entity.IdTradingPair)
                .Select(group => new KlineDataResponse
                {
                    IdTradingPair = group.Key,
                    Klines = group.Select(ToKline),
                });

        public static Kline ToKline(KlineDataEntity klineDataEntity) =>
            new()
            {
                OpenTime = klineDataEntity.OpenTime,
                OpenPrice = klineDataEntity.OpenPrice,
                HighPrice = klineDataEntity.HighPrice,
                LowPrice = klineDataEntity.LowPrice,
                ClosePrice = klineDataEntity.ClosePrice,
                Volume = klineDataEntity.Volume,
                CloseTime = klineDataEntity.CloseTime,
            };

        public static KlineDataEntity ToKlineDataEntity(KlineDataCreationRequest klineDataNew) =>
            new()
            {
                IdTradingPair = klineDataNew.IdTradingPair,
                OpenTime = klineDataNew.Kline.OpenTime,
                OpenPrice = klineDataNew.Kline.OpenPrice,
                HighPrice = klineDataNew.Kline.HighPrice,
                LowPrice = klineDataNew.Kline.LowPrice,
                ClosePrice = klineDataNew.Kline.ClosePrice,
                Volume = klineDataNew.Kline.Volume,
                CloseTime = klineDataNew.Kline.CloseTime,
            };
    }
}
