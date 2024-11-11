using AutoMapper;
using SVC_Coins.Models.Entities;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;

namespace SVC_Coins.Mappings;

/// <summary>
/// AutoMapper profile for mapping between input models and entity models.
/// </summary>
public class CoinsMappingProfile : Profile
{
    public CoinsMappingProfile()
    {
        // Mapping for creating/updating a coin
        CreateMap<CoinNew, CoinEntity>().ReverseMap();

        // Mapping from CoinEntity to Coin (output model)
        CreateMap<CoinEntity, Coin>()
            .ForMember(dest => dest.TradingPairs, opt => opt.MapFrom(src => src.TradingPairs));

        // Mapping from TradingPairEntity to TradingPair (output model)
        CreateMap<TradingPairEntity, TradingPair>()
            .ForMember(dest => dest.CoinQuote, opt => opt.MapFrom(src => src.CoinQuote));

        // Mapping from CoinEntity to Coin (for CoinQuote in TradingPair)
        CreateMap<CoinEntity, Coin>();
    }
}
