using AutoMapper;
using SVC_Kline.Models.Entities;
using SVC_Kline.Models.Input;

namespace SVC_Kline.Mappings;

/// <summary>
/// AutoMapper profile for mapping between input models and entity models.
/// </summary>
public class KlineDataMappingProfile : Profile
{
    public KlineDataMappingProfile() => CreateMap<KlineData, KlineDataEntity>().ReverseMap();
}
