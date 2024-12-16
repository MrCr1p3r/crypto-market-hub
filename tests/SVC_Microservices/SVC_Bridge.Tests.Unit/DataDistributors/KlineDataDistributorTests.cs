using AutoFixture;
using FluentAssertions;
using Moq;
using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.DataDistributors;
using SVC_Bridge.Models.Input;

namespace SVC_Bridge.Tests.Unit.DataDistributors;

public class KlineDataDistributorTests
{
    private readonly IFixture _fixture;
    private readonly Mock<ISvcCoinsClient> _mockCoinsClient;
    private readonly KlineDataDistributor _klineDataDistributor;

    public KlineDataDistributorTests()
    {
        _fixture = new Fixture();
        _mockCoinsClient = new Mock<ISvcCoinsClient>();

        _klineDataDistributor = new KlineDataDistributor(_mockCoinsClient.Object);
    }

    [Fact]
    public async Task InsertTradingPair_ShouldCall_InsertTradingPair_On_CoinsClient()
    {
        // Arrange
        var idCoinMain = _fixture.Create<int>();
        var idCoinQuote = _fixture.Create<int>();
        var expectedTradingPair = new TradingPairNew
        {
            IdCoinMain = idCoinMain,
            IdCoinQuote = idCoinQuote,
        };

        _mockCoinsClient
            .Setup(client => client.InsertTradingPair(It.IsAny<TradingPairNew>()))
            .ReturnsAsync(1);

        // Act
        await _klineDataDistributor.InsertTradingPair(idCoinMain, idCoinQuote);

        // Assert
        _mockCoinsClient.Verify(
            client =>
                client.InsertTradingPair(
                    It.Is<TradingPairNew>(tp =>
                        tp.IdCoinMain == expectedTradingPair.IdCoinMain
                        && tp.IdCoinQuote == expectedTradingPair.IdCoinQuote
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task InsertTradingPair_ShouldReturn_InsertedId_FromCoinsClient()
    {
        // Arrange
        var idCoinMain = _fixture.Create<int>();
        var idCoinQuote = _fixture.Create<int>();
        var expectedInsertedId = _fixture.Create<int>();

        _mockCoinsClient
            .Setup(client => client.InsertTradingPair(It.IsAny<TradingPairNew>()))
            .ReturnsAsync(expectedInsertedId);

        // Act
        var result = await _klineDataDistributor.InsertTradingPair(idCoinMain, idCoinQuote);

        // Assert
        result.Should().Be(expectedInsertedId);
    }
}
