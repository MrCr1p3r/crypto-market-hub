using AutoMapper;
using SVC_Kline.Models.Entities;
using SVC_Kline.Models.Input;
using SVC_Kline.Repositories.Interfaces;

namespace SVC_Kline.Repositories
{
    /// <summary>
    /// Repository for handling operations related to Kline data using Entity Framework and AutoMapper.
    /// </summary>
    /// <param name="context">The DbContext used for database operations.</param>
    /// <param name="mapper">The AutoMapper instance used for mapping models.</param>
    public class KlineDataRepository(KlineDataDbContext context, IMapper mapper) : IKlineDataRepository
    {
        private readonly KlineDataDbContext _context = context;
        private readonly IMapper _mapper = mapper;

        /// <summary>
        /// Inserts a new Kline data entry into the database.
        /// </summary>
        /// <param name="klineData">The KlineData object to insert.</param>
        public async Task InsertKlineData(KlineData klineData)
        {
            // Use AutoMapper to map the input model to the entity model
            var klineDataEntity = _mapper.Map<KlineDataEntity>(klineData);

            await _context.KlineData.AddAsync(klineDataEntity);
            await _context.SaveChangesAsync();
        }
    }
}
