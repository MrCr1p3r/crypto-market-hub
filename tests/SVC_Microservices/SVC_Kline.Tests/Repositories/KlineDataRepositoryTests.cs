using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SVC_Kline.Models.Entities;
using SVC_Kline.Models.Input;
using SVC_Kline.Repositories;

namespace SVC_Kline.Tests.Repositories;

public class KlineDataRepositoryTests
{
    private readonly IFixture _fixture;
    private readonly KlineDataDbContext _context;
    private readonly IMapper _mapper;
    private readonly KlineDataRepository _repository;

    public KlineDataRepositoryTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        // Set up the DbContext with an in-memory database
        var options = new DbContextOptionsBuilder<KlineDataDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        _context = new KlineDataDbContext(options);

        // Set up AutoMapper
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<KlineData, KlineDataEntity>();
        });
        _mapper = config.CreateMapper();

        // Initialize the repository with the in-memory context and AutoMapper
        _repository = new KlineDataRepository(_context, _mapper);
    }

    [Fact]
    public async Task InsertKlineData_ShouldAddEntityToDatabase()
    {
        // Arrange
        var klineData = _fixture.Create<KlineData>();
        var klineDataEntity = _mapper.Map<KlineDataEntity>(klineData);

        // Act
        await _repository.InsertKlineData(klineData);

        // Assert
        var entity = await _context.KlineData.FirstOrDefaultAsync(e => e.IdTradePair == klineData.IdTradePair);
        Assert.NotNull(entity);
        Assert.Equal(klineDataEntity.OpenPrice, entity.OpenPrice);
    }
}
