using Microsoft.EntityFrameworkCore;
using SVC_Coins.Domain.Entities;
using SVC_Coins.Infrastructure;
using SVC_Coins.Repositories.Interfaces;

namespace SVC_Coins.Repositories;

/// <summary>
/// Repository for handling database operations related to exchanges.
/// </summary>
/// <param name="context">The DbContext used for database operations.</param>
public class ExchangesRepository(CoinsDbContext context) : IExchangesRepository
{
    private readonly CoinsDbContext _context = context;

    /// <inheritdoc />
    public async Task<IEnumerable<ExchangesEntity>> GetAllExchanges() =>
        await _context.Exchanges.ToListAsync();
}
