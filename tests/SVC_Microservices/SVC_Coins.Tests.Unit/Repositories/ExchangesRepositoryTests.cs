using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SVC_Coins.Domain.Entities;
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
        _seedContext.Database.EnsureCreated();

        _testedRepository = new ExchangesRepository(_actContext);
    }

    [Fact]
    public async Task GetAllExchanges_ReturnsAllExchanges()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var result = await _testedRepository.GetAllExchanges();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(e => e.Name == "Binance");
        result.Should().Contain(e => e.Name == "Bybit");
        result.Should().Contain(e => e.Name == "Kraken");
    }

    [Fact]
    public async Task GetAllExchanges_WhenNoExchangesExist_ReturnsEmpty()
    {
        // Act
        var result = await _testedRepository.GetAllExchanges();

        // Assert
        result.Should().BeEmpty();
    }

    private async Task SeedDatabase()
    {
        await _seedContext.Exchanges.AddRangeAsync(TestData.GetExchanges());
        await _seedContext.SaveChangesAsync();
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

    public static class TestData
    {
        public static IEnumerable<ExchangesEntity> GetExchanges() =>
            [
                new ExchangesEntity { Name = "Binance" },
                new ExchangesEntity { Name = "Bybit" },
                new ExchangesEntity { Name = "Kraken" },
            ];
    }
}
