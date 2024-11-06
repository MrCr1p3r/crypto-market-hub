using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SVC_Kline.Models.Entities;
using SVC_Kline.Models.Input;
using SVC_Kline.Repositories.Interfaces;

namespace SVC_Kline.Repositories;

/// <summary>
/// Repository for handling operations related to Kline data using Entity Framework and AutoMapper.
/// </summary>
/// <param name="context">The DbContext used for database operations.</param>
/// <param name="mapper">The AutoMapper instance used for mapping models.</param>
public class KlineDataRepository(KlineDataDbContext context, IMapper mapper) : IKlineDataRepository
{
    private readonly KlineDataDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    /// <inheritdoc />
    public async Task InsertKlineData(KlineData klineData)
    {
        var klineDataEntity = _mapper.Map<KlineDataEntity>(klineData);
        await _context.KlineData.AddAsync(klineDataEntity);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task InsertManyKlineData(IEnumerable<KlineData> klineDataList)
    {
        var klineDataEntities = _mapper.Map<IEnumerable<KlineDataEntity>>(klineDataList);
        await _context.KlineData.AddRangeAsync(klineDataEntities);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<KlineData>> GetAllKlineData()
    {
        var klineDataEntities = await _context.KlineData.ToListAsync();
        return _mapper.Map<IEnumerable<KlineData>>(klineDataEntities);
    }

    /// <inheritdoc />
    public async Task DeleteKlineDataForTradingPair(int idTradePair)
    {
        var klineDataEntities = _context.KlineData.Where(k => k.IdTradePair == idTradePair);
        _context.KlineData.RemoveRange(klineDataEntities);
        await _context.SaveChangesAsync();
    }
}
