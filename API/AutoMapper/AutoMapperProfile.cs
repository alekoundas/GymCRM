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
            CreateMap<TrainGroup, TrainGroupDto>()
               .ForMember(destination => destination.Duration, act => act.MapFrom(source =>
                    source.Duration != null
                        ? new DateTime(2000, 1, 1, source.Duration.Hours, source.Duration.Minutes, 0)
                        : new DateTime(2000, 1, 1, 0, 0, 0)))
               .ForMember(destination => destination.StartOn, act => act.MapFrom(source =>
                    source.StartOn != null
                        ? new DateTime(2000, 1, 1, source.StartOn.Hours, source.StartOn.Minutes, 0)
                        : new DateTime(2000, 1, 1, 0, 0, 0)));
            CreateMap<TrainGroupDto, TrainGroup>()
               .ForMember(destination => destination.Duration, act => act.MapFrom(source => source.Duration.TimeOfDay))
               .ForMember(destination => destination.StartOn, act => act.MapFrom(source => source.StartOn.TimeOfDay))
               .ForMember(destination => destination.TrainerId, act => act.MapFrom(source => new Guid(source.TrainerId!)));

            // TrainGroupAddDto
            CreateMap<TrainGroup, TrainGroupAddDto>()
               .ForMember(destination => destination.Duration, act => act.MapFrom(source =>
                    source.Duration != null
                        ? new DateTime(2000, 1, 1, source.Duration.Hours, source.Duration.Minutes, 0)
                        : new DateTime(2000, 1, 1, 0, 0, 0)))
               .ForMember(destination => destination.StartOn, act => act.MapFrom(source =>
                    source.StartOn != null
                        ? new DateTime(2000, 1, 1, source.StartOn.Hours, source.StartOn.Minutes, 0)
                        : new DateTime(2000, 1, 1, 0, 0, 0)));
            CreateMap<TrainGroupAddDto, TrainGroup>()
                .ForMember(destination => destination.Duration, act => act.MapFrom(source => source.Duration.TimeOfDay))
                .ForMember(destination => destination.StartOn, act => act.MapFrom(source => source.StartOn.TimeOfDay))
                .ForMember(destination => destination.TrainerId, act => act.MapFrom(source => new Guid(source.TrainerId!)));


            // TrainGroupDateDto
            CreateMap<TrainGroupDate, TrainGroupDateDto>()
                .ForMember(dest => dest.FixedDay, opt => opt.MapFrom<TrainGroupDateDtoDateOnlyToDateTimeResolver>());
            CreateMap<TrainGroupDateDto, TrainGroupDate>()
                .ForMember(dest => dest.FixedDay, opt => opt.MapFrom<TrainGroupDateDtoDateTimeToDateOnlyResolver>());

            // TrainGroupDateAddDto
            CreateMap<TrainGroupDate, TrainGroupDateAddDto>()
                .ForMember(dest => dest.FixedDay, opt => opt.MapFrom<TrainGroupDateAddDtoDateOnlyToDateTimeResolver>());
            CreateMap<TrainGroupDateAddDto, TrainGroupDate>()
                .ForMember(dest => dest.FixedDay, opt => opt.MapFrom<TrainGroupDateAddDtoDateTimeToDateOnlyResolver>());


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
