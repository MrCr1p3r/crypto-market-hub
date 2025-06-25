using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Enums;
using SVC_Coins.Infrastructure;
using SVC_Coins.Repositories;

namespace SVC_Coins.Tests.Unit.Repositories;

public class ExchangesRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CoinsDbContext _seedContext;
    private readonly CoinsDbContext _actContext;
    private readonly CoinsDbContext _assertContext;
    private readonly ExchangesRepository _testedRepository;

    public ExchangesRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CoinsDbContext>().UseSqlite(_connection).Options;

        _seedContext = new CoinsDbContext(options);
        _actContext = new CoinsDbContext(options);
        _assertContext = new CoinsDbContext(options);
        _seedContext.Database.EnsureCreated(); // This automatically seeds exchanges

        _testedRepository = new ExchangesRepository(_actContext);
    }

    [Fact]
    public async Task GetAllExchanges_ReturnsAllExchanges()
    {
        // Arrange - No manual seeding needed, exchanges are automatically seeded

        // Act
        var result = await _testedRepository.GetAllExchanges();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(e => e.Name == "Binance");
        result.Should().Contain(e => e.Name == "Bybit");
        result.Should().Contain(e => e.Name == "Mexc");

        // Verify the exchanges have the correct IDs from the enum
        result.Should().Contain(e => e.Id == (int)Exchange.Binance && e.Name == "Binance");
        result.Should().Contain(e => e.Id == (int)Exchange.Bybit && e.Name == "Bybit");
        result.Should().Contain(e => e.Id == (int)Exchange.Mexc && e.Name == "Mexc");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _seedContext.Dispose();
            _actContext.Dispose();
            _assertContext.Dispose();
            _connection.Dispose();
        }
    }
}
