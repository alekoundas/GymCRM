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
using Core.Dtos.TrainGroupΑttendance;
using Core.Dtos.User;
using Core.Dtos.UserRole;
using Core.Dtos.UserStatus;
using Core.Dtos.WorkoutPlan;
using Core.Dtos.WorkoutPlanRecording;
using Core.Dtos.WorkoutPlanRule;
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
          

            // TrainGroupPresence mappings.
            CreateMap<TrainGroupΑttendance, TrainGroupΑttendanceDto>();
            CreateMap<TrainGroupΑttendanceDto, TrainGroupΑttendance>();

            CreateMap<TrainGroupΑttendance, TrainGroupΑttendanceAddDto>();
            CreateMap<TrainGroupΑttendanceAddDto, TrainGroupΑttendance>();

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

            // WorkoutPlanRule mappings.
            CreateMap<WorkoutPlanRule, WorkoutPlanRuleDto>()
                .ForMember(x => x.WeekCount, o => o.MapFrom(x => x.Weeks.Count))
                .ForMember(x => x.WorkoutPlanCount, o => o.MapFrom(x => x.WorkoutPlans.Count))
                .ForMember(x => x.IsLocked, o => o.Ignore());
            CreateMap<WorkoutPlanRuleDto, WorkoutPlanRule>()
                .ForMember(x => x.WorkoutPlans, o => o.Ignore());

            CreateMap<WorkoutPlanRule, WorkoutPlanRuleAddDto>();
            CreateMap<WorkoutPlanRuleAddDto, WorkoutPlanRule>()
                .ForMember(x => x.WorkoutPlans, o => o.Ignore());

            CreateMap<WorkoutPlanRuleWeek, WorkoutPlanRuleWeekDto>();
            CreateMap<WorkoutPlanRuleWeekDto, WorkoutPlanRuleWeek>();

            CreateMap<WorkoutPlanRuleWeek, WorkoutPlanRuleWeekAddDto>();
            CreateMap<WorkoutPlanRuleWeekAddDto, WorkoutPlanRuleWeek>();

            // WorkoutPlanRecording mappings. The derived flags are filled in by the
            // recording service, which owns the cut-off rule.
            CreateMap<WorkoutPlanRecording, WorkoutPlanRecordingDto>()
                .ForMember(x => x.IsRunning, o => o.Ignore())
                .ForMember(x => x.IsIncomplete, o => o.Ignore())
                .ForMember(x => x.ElapsedSeconds, o => o.Ignore());
            CreateMap<WorkoutPlanRecordingDto, WorkoutPlanRecording>()
                .ForMember(x => x.User, o => o.Ignore())
                .ForMember(x => x.WorkoutPlan, o => o.Ignore());

            // WorkoutPlan mappings.
            CreateMap<WorkoutPlan, WorkoutPlanDto>()
                .ForMember(x => x.WorkoutPlanRuleName, o => o.MapFrom(x => x.WorkoutPlanRule != null ? x.WorkoutPlanRule.Name : string.Empty))
                .ForMember(x => x.WeekCount, o => o.MapFrom(x => x.WorkoutPlanRule != null ? x.WorkoutPlanRule.Weeks.Count : 0))
                .ForMember(x => x.CurrentWeekMessage, o => o.Ignore())
                .ForMember(x => x.IsRunning, o => o.Ignore())
                .ForMember(x => x.ElapsedSeconds, o => o.Ignore())
                .ForMember(x => x.HasIncompleteRecording, o => o.Ignore())
                .ForMember(x => x.LastRecordingOn, o => o.Ignore());
            CreateMap<WorkoutPlanDto, WorkoutPlan>()
                .ForMember(x => x.WorkoutPlanRule, o => o.Ignore())
                .ForMember(x => x.Recordings, o => o.Ignore());

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
