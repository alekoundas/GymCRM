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
            CreateMap<TrainGroupDto, TrainGroup>();

            // TrainGroupAddDto
            CreateMap<TrainGroup, TrainGroupAddDto>();
            CreateMap<TrainGroupAddDto, TrainGroup>();

            // TrainGroupDateDto
            CreateMap<TrainGroupDate, TrainGroupDateDto>();
            CreateMap<TrainGroupDateDto, TrainGroupDate>();

            // TrainGroupDateAddDto
            CreateMap<TrainGroupDate, TrainGroupDateAddDto>();
            CreateMap<TrainGroupDateAddDto, TrainGroupDate>();
                //.ForMember(dest => dest.FixedDay, opt => opt.MapFrom<TrainGroupDateAddDtoDateTimeToDateOnlyResolver>());


            CreateMap<ContactInformation, ContactInformationDto>();
            CreateMap<ContactInformationDto, ContactInformation>();

            CreateMap<TrainGroupParticipant, TrainGroupParticipantDto>();
            CreateMap<TrainGroupParticipantDto, TrainGroupParticipant>();

            CreateMap<TrainGroupDateCancellationSubscriber, TrainGroupCancellationSubscriberDto>();
            CreateMap<TrainGroupCancellationSubscriberDto, TrainGroupDateCancellationSubscriber>();


            // Identity mappings.
            CreateMap<IdentityRole<Guid>, IdentityRoleDto>();
            CreateMap<IdentityRoleDto, IdentityRole<Guid>>();

            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
        }
    }
}
