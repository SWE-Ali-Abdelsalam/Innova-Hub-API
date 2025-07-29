using AutoMapper;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;

namespace InnoHub.Helper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {

            CreateMap<Best_SeillingResponseDTO, ProductViewModel>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(o => o.AuthorId)) // Map AuthorId to AuthorName
                .ReverseMap();
            CreateMap<DeliveryMethod,DeliveryMethodDTO>().ReverseMap();
            CreateMap<ShippingAddress,ShippingAddressDTO>().ReverseMap();
      
        }
    }
}
