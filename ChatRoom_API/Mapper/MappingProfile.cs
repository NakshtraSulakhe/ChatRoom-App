using AutoMapper;
using ChatRoom_API.Models;
using ChatRoom_API.DTOs;

namespace ChatRoom_API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<MessageDto, ChatMessage>().ReverseMap();

            CreateMap<PrivateMessageDto, PrivateMessage>()
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
                .ReverseMap();
        }
    }
}
