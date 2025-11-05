using AutoMapper;
using Core.Dtos;
using Core.Dtos.Exercise;
using Core.Dtos.ExerciseHistory;
using Core.Dtos.Identity;
using Core.Dtos.Mail;
using Core.Dtos.PhoneNumber;
using Core.Dtos.TrainGroup;
using Core.Dtos.TrainGroupDate;
using Core.Dtos.TrainGroupParticipantUnavailableDate;
using Core.Dtos.TrainGroupUnavailableDate;
using Core.Dtos.User;
using Core.Dtos.UserRole;
using Core.Dtos.UserStatus;
using Core.Dtos.WorkoutPlan;
using Core.Models;
using Microsoft.AspNetCore.Identity;

namespace API.AutoMapper
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // TrainGroup mappings.
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

            // TrainGroupDate mappings.
            CreateMap<TrainGroupDate, TrainGroupDateDto>();
            CreateMap<TrainGroupDateDto, TrainGroupDate>();

            CreateMap<TrainGroupDate, TrainGroupDateAddDto>();
            CreateMap<TrainGroupDateAddDto, TrainGroupDate>();
          

            // TrainGroupParticipant mappings.
            CreateMap<TrainGroupParticipant, TrainGroupParticipantDto>();
            CreateMap<TrainGroupParticipantDto, TrainGroupParticipant>();

            CreateMap<TrainGroupParticipant, TrainGroupParticipantAddDto>();
            CreateMap<TrainGroupParticipantAddDto, TrainGroupParticipant>();

            // TrainGroupUnavailableDate mappings.
            CreateMap<TrainGroupUnavailableDate, TrainGroupUnavailableDateDto>();
            CreateMap<TrainGroupUnavailableDateDto, TrainGroupUnavailableDate>();

            CreateMap<TrainGroupUnavailableDate, TrainGroupUnavailableDateAddDto>();
            CreateMap<TrainGroupUnavailableDateAddDto, TrainGroupUnavailableDate>();

            // TrainGroupParticipantUnavailableDate mappings.
            CreateMap<TrainGroupParticipantUnavailableDate, TrainGroupParticipantUnavailableDateDto>();
            CreateMap<TrainGroupParticipantUnavailableDateDto, TrainGroupParticipantUnavailableDate>();

            CreateMap<TrainGroupParticipantUnavailableDate, TrainGroupParticipantUnavailableDateAddDto>();
            CreateMap<TrainGroupParticipantUnavailableDateAddDto, TrainGroupParticipantUnavailableDate>();


            // PhoneNumber mappings.
            CreateMap<PhoneNumber, PhoneNumberDto>();
            CreateMap<PhoneNumberDto, PhoneNumber>();

            CreateMap<PhoneNumber, PhoneNumberAddDto>();
            CreateMap<PhoneNumberAddDto, PhoneNumber>();


            // Mail mappings.
            CreateMap<Mail, MailDto>();
            CreateMap<MailDto, Mail>();

            CreateMap<Mail, MailAddDto>();
            CreateMap<MailAddDto, Mail>();

            // Exercise mappings.
            CreateMap<Exercise, ExerciseDto>();
            CreateMap<ExerciseDto, Exercise>();

            CreateMap<Exercise, ExerciseAddDto>();
            CreateMap<ExerciseAddDto, Exercise>();

            // WorkoutPlan mappings.
            CreateMap<WorkoutPlan, WorkoutPlanDto>();
            CreateMap<WorkoutPlanDto, WorkoutPlan>();

            CreateMap<WorkoutPlan, WorkoutPlanAddDto>();
            CreateMap<WorkoutPlanAddDto, WorkoutPlan>();


            // UserStatus mappings.
            CreateMap<UserStatus, UserStatusDto>();
            CreateMap<UserStatusDto, UserStatus>();

            CreateMap<UserStatus, UserStatusAddDto>();
            CreateMap<UserStatusAddDto, UserStatus>();


            // ExerciseHistory mappings.
            CreateMap<ExerciseHistory, ExerciseHistoryDto>();
            CreateMap<ExerciseHistoryDto, ExerciseHistory>();


            // Mail mappings.
            CreateMap<Mail, MailDto>();
            CreateMap<MailDto, Mail>();

            CreateMap<Mail, MailAddDto>();
            CreateMap<MailAddDto, Mail>();




            // Identity mappings.
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();

            CreateMap<UserRole, UserRoleDto>();
            CreateMap<UserRoleDto, UserRole>();

            CreateMap<Role, RoleDto>();
            CreateMap<RoleDto, Role>();

        }
    }
}
