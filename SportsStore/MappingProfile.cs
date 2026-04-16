using AutoMapper;
using SportsStore.Models.DTOs;
using SportsStore.Services;
using SportsStore.Services.Messaging;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<OrderDto, OrderViewDto>();
        CreateMap<OrderViewDto, OrderDto>();

        CreateMap<OrderViewDto, OrderWorkflowMessage>()
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        CreateMap<OrderItemViewDto, OrderWorkflowItemMessage>();
    }
}