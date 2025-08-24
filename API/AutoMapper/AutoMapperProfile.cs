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
            CreateMap<TrainGroup, TrainGroupDto>();
            CreateMap<TrainGroupDto, TrainGroup>()
               .ForMember(destination => destination.Duration, act => act.MapFrom(source => source.Duration.TimeOfDay))
               .ForMember(destination => destination.StartOn, act => act.MapFrom(source => source.StartOn.TimeOfDay));

            CreateMap<TrainGroup, TrainGroupAddDto>();
            CreateMap<TrainGroupAddDto, TrainGroup>()
                .ForMember(destination => destination.Duration, act => act.MapFrom(source => source.Duration.TimeOfDay))
                .ForMember(destination => destination.StartOn, act => act.MapFrom(source => source.StartOn.TimeOfDay));


            CreateMap<TrainGroupDate, TrainGroupDateDto>();
            CreateMap<TrainGroupDateDto, TrainGroupDate>();




            CreateMap<TrainGroupDate, TrainGroupDateAddDto>();
            CreateMap<TrainGroupDateAddDto, TrainGroupDate>()
                .ForMember(dest => dest.FixedDay, opt => opt.MapFrom<FixedDayResolver>());
                            //.ForMember(dest => dest.FixedDay, opt => opt.MapFrom(source =>
                            // {
                            //     if (source.FixedDay.HasValue)
                            //         return DateOnly.FromDateTime(source.FixedDay.Value);

                            //     return null;
                            // }));


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
