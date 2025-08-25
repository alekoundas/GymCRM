using AutoMapper;
using Core.Dtos;
using Core.Dtos.Identity;
using Core.Dtos.TrainGroup;
using Core.Dtos.TrainGroupDate;
using Core.Models;
using Microsoft.AspNetCore.Identity;

namespace API.AutoMapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // TrainGroupDto
            CreateMap<TrainGroup, TrainGroupDto>();
            CreateMap<TrainGroupDto, TrainGroup>()
               .ForMember(destination => destination.Duration, act => act.MapFrom(source => source.Duration.TimeOfDay))
               .ForMember(destination => destination.StartOn, act => act.MapFrom(source => source.StartOn.TimeOfDay))
               .ForMember(destination => destination.TrainerId, act => act.MapFrom(source => new Guid(source.TrainerId!)));

            // TrainGroupAddDto
            CreateMap<TrainGroup, TrainGroupAddDto>();
            CreateMap<TrainGroupAddDto, TrainGroup>()
                .ForMember(destination => destination.Duration, act => act.MapFrom(source => source.Duration.TimeOfDay))
                .ForMember(destination => destination.StartOn, act => act.MapFrom(source => source.StartOn.TimeOfDay))
                .ForMember(destination => destination.TrainerId, act => act.MapFrom(source => new Guid(source.TrainerId!)));


            // TrainGroupDateDto
            CreateMap<TrainGroupDate, TrainGroupDateDto>();
            CreateMap<TrainGroupDateDto, TrainGroupDate>()
                .ForMember(dest => dest.FixedDay, opt => opt.MapFrom<TrainGroupDateDtoDateOnlyResolver>());

            // TrainGroupDateAddDto
            CreateMap<TrainGroupDate, TrainGroupDateAddDto>();
            CreateMap<TrainGroupDateAddDto, TrainGroupDate>()
                .ForMember(dest => dest.FixedDay, opt => opt.MapFrom<TrainGroupDateAddDtoDateOnlyResolver>());


            CreateMap<ContactInformation, ContactInformationDto>();
            CreateMap<ContactInformationDto, ContactInformation>();

            CreateMap<TrainGroupParticipant, TrainGroupParticipantDto>();
            CreateMap<TrainGroupParticipantDto, TrainGroupParticipant>();

            CreateMap<TrainGroupCancellationSubscriber, TrainGroupCancellationSubscriberDto>();
            CreateMap<TrainGroupCancellationSubscriberDto, TrainGroupCancellationSubscriber>();


            // Identity mappings.
            CreateMap<IdentityRole<Guid>, IdentityRoleDto>();
            CreateMap<IdentityRoleDto, IdentityRole<Guid>>();

            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
        }
    }
}
