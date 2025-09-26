using AutoMapper;
using Core.Dtos;
using Core.Dtos.Identity;
using Core.Dtos.Mail;
using Core.Dtos.PhoneNumber;
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
            // TrainGroup
            CreateMap<TrainGroup, TrainGroupDto>();
            CreateMap<TrainGroupDto, TrainGroup>();

            CreateMap<TrainGroup, TrainGroupAddDto>();
            CreateMap<TrainGroupAddDto, TrainGroup>()
                .AfterMap((src, dest) =>
                {
                    // Set TrainGroupId for all TrainGroupParticipants
                    foreach (TrainGroupParticipant participant in dest.TrainGroupDates.SelectMany(x => x.TrainGroupParticipants))
                        participant.TrainGroup = dest; //Set navigation property
                });

            // TrainGroupDate
            CreateMap<TrainGroupDate, TrainGroupDateDto>();
            CreateMap<TrainGroupDateDto, TrainGroupDate>();

            CreateMap<TrainGroupDate, TrainGroupDateAddDto>();
            CreateMap<TrainGroupDateAddDto, TrainGroupDate>();
            //.ForMember(dest => dest.FixedDay, opt => opt.MapFrom<TrainGroupDateAddDtoDateTimeToDateOnlyResolver>());

            // PhoneNumber
            CreateMap<PhoneNumber, PhoneNumberDto>();
            CreateMap<PhoneNumberDto, PhoneNumber>();

            CreateMap<PhoneNumber, PhoneNumberAddDto>();
            CreateMap<PhoneNumberAddDto, PhoneNumber>();

            // TrainGroupParticipant
            CreateMap<TrainGroupParticipant, TrainGroupParticipantDto>();
            CreateMap<TrainGroupParticipantDto, TrainGroupParticipant>();

            CreateMap<TrainGroupParticipant, TrainGroupParticipantAddDto>();
            CreateMap<TrainGroupParticipantAddDto, TrainGroupParticipant>();


            //CreateMap<TrainGroupDateCancellationSubscriber, TrainGroupCancellationSubscriberDto>();
            //CreateMap<TrainGroupCancellationSubscriberDto, TrainGroupDateCancellationSubscriber>();

            CreateMap<Mail, MailDto>();
            CreateMap<MailDto, Mail>();

            CreateMap<Mail, MailAddDto>();
            CreateMap<MailAddDto, Mail>();

            // Identity mappings.
            CreateMap<Role, IdentityRoleDto>();
            CreateMap<IdentityRoleDto, Role>();

            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
        }
    }
}
