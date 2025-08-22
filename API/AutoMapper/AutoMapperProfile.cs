using AutoMapper;
using Core.Dtos;
using Core.Dtos.Identity;
using Core.Models;
using Microsoft.AspNetCore.Identity;

namespace API.AutoMapper
{
    public class AutoMapperProfile: Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<TrainGroup, TrainGroupDto>();
            CreateMap<TrainGroupDto, TrainGroup>();

            CreateMap<TrainGroupDate, TrainGroupDateDto>();
            CreateMap<TrainGroupDateDto, TrainGroupDate>();

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
