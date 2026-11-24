using System.Collections;
using System.Reflection;
using System.Text;
using API.Tests.Host;
using AutoMapper;
using AutoMapper.Internal;
using Core.Dtos.Subscription;
using Core.Dtos.TrainGroup;
using Core.Dtos.TrainGroupDate;
using Core.Dtos.User;
using Core.Dtos.WorkoutPlan;
using Core.Dtos.WorkoutPlanRule;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Tests.Shared;

namespace API.Tests.AutoMapper
{
    /// <summary>
    /// What becomes of a row on its way out and of a form on its way in.
    /// </summary>
    public sealed class AutoMapperProfileTests : IClassFixture<ApiFixture>
    {
        private readonly IMapper _mapper;

        public AutoMapperProfileTests(ApiFixture api)
        {
            _mapper = api.Services.GetRequiredService<IMapper>();
        }

        private IEnumerable<TypeMap> Maps()
        {
            return _mapper.ConfigurationProvider.Internal().GetAllTypeMaps().OrderBy(x => x.SourceType.Name, StringComparer.Ordinal).ThenBy(x => x.DestinationType.Name, StringComparer.Ordinal);
        }


        // ================================================================
        // Every map there is
        // ================================================================

        // Where each value of each map comes from, and which are left alone. A property that is added,
        // renamed or taken away shows here, in every map that carries it.
        [Fact]
        public void Every_map_carries_what_was_approved()
        {
            StringBuilder text = new StringBuilder();

            foreach (TypeMap map in Maps())
            {
                text.AppendLine($"{map.SourceType.Name} -> {map.DestinationType.Name}");

                foreach (PropertyMap property in map.PropertyMaps.OrderBy(x => x.DestinationName, StringComparer.Ordinal))
                    text.AppendLine($"  {property.DestinationName}: {Describe(property)}");

                foreach (string name in map.GetUnmappedPropertyNames().Order(StringComparer.Ordinal))
                    text.AppendLine($"  {name}: not set");

                text.AppendLine();
            }

            ApprovedFile.ShouldMatch(text.ToString(), "Maps");
        }

        private static string Describe(PropertyMap property)
        {
            if (property.Ignored)
                return "ignored";

            if (property.CustomMapExpression != null)
                return property.CustomMapExpression.Body.ToString();

            return string.Join(".", property.SourceMembers.Select(x => x.Name));
        }

        // Filled from top to bottom and mapped: each value with the same name and type arrives.
        [Fact]
        public void Every_plain_value_arrives()
        {
            int values = 0;

            foreach (TypeMap map in Maps())
            {
                object source = Filled(map.SourceType);
                object result = _mapper.Map(source, map.SourceType, map.DestinationType);

                foreach (PropertyMap property in map.PropertyMaps.Where(x => !x.Ignored && x.CustomMapExpression == null && x.SourceMembers.Length == 1))
                {
                    PropertyInfo from = (PropertyInfo)property.SourceMembers[0];
                    PropertyInfo to = (PropertyInfo)property.DestinationMember;

                    if (from.PropertyType != to.PropertyType || !IsPlain(from.PropertyType))
                        continue;

                    to.GetValue(result).ShouldBe(from.GetValue(source), $"{map.SourceType.Name}.{from.Name} -> {map.DestinationType.Name}.{to.Name}");
                    values++;
                }
            }

            values.ShouldBeGreaterThan(300);
        }

        private static object Filled(Type type)
        {
            object instance = Activator.CreateInstance(type)!;
            int number = 0;

            foreach (PropertyInfo property in type.GetProperties().Where(x => x.CanWrite && IsPlain(x.PropertyType)))
                property.SetValue(instance, Value(property, ++number));

            // The ids of members are text in the dtos, and have to be ids.
            foreach (PropertyInfo property in type.GetProperties().Where(x => x.CanWrite && x.PropertyType == typeof(string) && x.Name is "Id" or "UserId" or "TrainerId" or "RoleId"))
                property.SetValue(instance, Guid.NewGuid().ToString());

            return instance;
        }

        private static bool IsPlain(Type type)
        {
            Type plain = Nullable.GetUnderlyingType(type) ?? type;

            return plain.IsPrimitive || plain.IsEnum || plain == typeof(string) || plain == typeof(Guid) || plain == typeof(DateTime) || plain == typeof(DateTimeOffset) || plain == typeof(byte[]);
        }

        private static object Value(PropertyInfo property, int number)
        {
            Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (type == typeof(string))
                return $"{property.Name} {number}";
            if (type == typeof(bool))
                return true;
            if (type == typeof(Guid))
                return Guid.NewGuid();
            if (type == typeof(DateTime))
                return new DateTime(2031, 1, 1).AddHours(number);
            if (type == typeof(DateTimeOffset))
                return new DateTimeOffset(2031, 1, 1, number % 24, 0, 0, TimeSpan.Zero);
            if (type == typeof(byte[]))
                return new byte[] { (byte)number, 2 };
            if (type.IsEnum)
                return Enum.GetValues(type).Cast<object>().Last();

            return Convert.ChangeType(number, type);
        }

        // A list left out, or null, is an empty one; a row that is not there is nothing.
        [Fact]
        public void Nothing_is_nothing_and_no_list_is_an_empty_one()
        {
            foreach (TypeMap map in Maps())
            {
                _mapper.Map(null, map.SourceType, map.DestinationType).ShouldBeNull();

                object source = Filled(map.SourceType);
                foreach (PropertyInfo list in map.SourceType.GetProperties().Where(IsList))
                    list.SetValue(source, null);

                object result = _mapper.Map(source, map.SourceType, map.DestinationType);

                foreach (PropertyInfo list in map.DestinationType.GetProperties().Where(IsList).Where(x => map.SourceType.GetProperty(x.Name) != null && map.PropertyMaps.Any(y => y.DestinationName == x.Name && !y.Ignored)))
                    ((IEnumerable)list.GetValue(result)!).Cast<object>().ShouldBeEmpty($"{map.SourceType.Name} -> {map.DestinationType.Name}.{list.Name}");
            }
        }

        private static bool IsList(PropertyInfo property)
        {
            return property.CanWrite && property.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(property.PropertyType);
        }


        // ================================================================
        // How deep they go
        // ================================================================

        // This version of AutoMapper follows a map that leads back to itself for as deep as the data goes,
        // and when the stack runs out the whole app is down (three advisories, all of them this). There is
        // no such map here, and there must not be one.
        [Fact]
        public void No_map_leads_back_to_itself()
        {
            HashSet<TypeMap> done = new HashSet<TypeMap>();

            foreach (TypeMap map in Maps())
                ShouldEnd(map, [], done);

            done.Count.ShouldBe(Maps().Count());
        }

        private void ShouldEnd(TypeMap map, List<TypeMap> way, HashSet<TypeMap> done)
        {
            way.ShouldNotContain(map, string.Join(", ", way.Append(map).Select(x => $"{x.SourceType.Name} -> {x.DestinationType.Name}")));

            foreach (TypeMap next in Next(map))
                ShouldEnd(next, [.. way, map], done);

            done.Add(map);
        }

        // The maps it goes on to, for the rows and the lists within.
        private IEnumerable<TypeMap> Next(TypeMap map)
        {
            return map.MemberMaps
                .Where(x => !x.Ignored && x.SourceType != null)
                .Select(x => _mapper.ConfigurationProvider.Internal().ResolveTypeMap(Within(x.SourceType), Within(x.DestinationType)))
                .OfType<TypeMap>()
                .Distinct();
        }

        private static Type Within(Type type)
        {
            if (type.IsArray)
                return type.GetElementType()!;

            return type.IsGenericType ? type.GetGenericArguments().Last() : type;
        }

        // The ways to it that the test above could not see.
        [Fact]
        public void No_map_is_made_out_as_it_goes()
        {
            foreach (TypeMap map in Maps())
            {
                string name = $"{map.SourceType.Name} -> {map.DestinationType.Name}";

                map.HasTypeConverter.ShouldBeFalse(name);
                map.IncludedDerivedTypes.ShouldBeEmpty(name);
                map.IncludedBaseTypes.ShouldBeEmpty(name);
                map.MemberMaps.Where(x => !x.Ignored).SelectMany(x => new[] { x.SourceType, x.DestinationType }).Where(IsOpen).ShouldBeEmpty(name);
            }
        }

        // Whatever is put in it.
        private static bool IsOpen(Type type)
        {
            Type within = Within(type);

            return within == typeof(object) || within.IsInterface || typeof(IDictionary).IsAssignableFrom(type);
        }


        // ================================================================
        // Ids in text
        // ================================================================

        [Fact]
        public void The_id_of_a_member_is_text_in_a_form()
        {
            Guid id = Guid.NewGuid();

            _mapper.Map<TrainGroupParticipant>(new TrainGroupParticipantAddDto { UserId = id.ToString() }).UserId.ShouldBe(id);
            _mapper.Map<TrainGroupParticipantDto>(new TrainGroupParticipant { UserId = id }).UserId.ShouldBe(id.ToString());
            Should.Throw<AutoMapperMappingException>(() => _mapper.Map<TrainGroupParticipant>(new TrainGroupParticipantAddDto { UserId = "x" }));
            Should.Throw<AutoMapperMappingException>(() => _mapper.Map<TrainGroupParticipant>(new TrainGroupParticipantAddDto { UserId = "" }));
        }

        [Fact]
        public void A_row_with_no_id_yet_is_nought()
        {
            _mapper.Map<WorkoutPlan>(new WorkoutPlanDto { UserId = Guid.NewGuid().ToString() }).Id.ShouldBe(0);
            _mapper.Map<User>(new UserDto { Id = null }).Id.ShouldBe(Guid.Empty);
        }


        // ================================================================
        // Groups
        // ================================================================

        // Members that come with a new group's dates are in the group, which has no id yet.
        [Fact]
        public void Members_of_a_new_groups_dates_are_in_the_group()
        {
            string userId = Guid.NewGuid().ToString();

            TrainGroup group = _mapper.Map<TrainGroup>(new TrainGroupAddDto
            {
                TrainerId = Guid.NewGuid().ToString(),
                TrainGroupDates =
                [
                    new TrainGroupDateAddDto { TrainGroupParticipants = [new TrainGroupParticipantAddDto { UserId = userId }, new TrainGroupParticipantAddDto { UserId = userId }] },
                    new TrainGroupDateAddDto { TrainGroupParticipants = [new TrainGroupParticipantAddDto { UserId = userId }] },
                    new TrainGroupDateAddDto()
                ]
            });

            List<TrainGroupParticipant> participants = group.TrainGroupDates.SelectMany(x => x.TrainGroupParticipants).ToList();
            participants.Count.ShouldBe(3);
            participants.ShouldAllBe(x => ReferenceEquals(x.TrainGroup, group));
        }

        // Saving a group must not touch anybody's account.
        [Fact]
        public void The_trainer_never_comes_back_through_a_group()
        {
            TrainGroup group = _mapper.Map<TrainGroup>(new TrainGroupDto { TrainerId = Guid.NewGuid().ToString(), Trainer = new UserDto { Id = Guid.NewGuid().ToString(), FirstName = "Changed" } });

            group.Trainer.ShouldBeNull();
            _mapper.Map<TrainGroupDto>(new TrainGroup { Trainer = new User { FirstName = "Νίκος" } }).Trainer.ShouldNotBeNull().FirstName.ShouldBe("Νίκος");
        }


        // ================================================================
        // Plans
        // ================================================================

        [Fact]
        public void A_plan_brings_the_name_of_its_rule_and_how_many_weeks_it_has()
        {
            WorkoutPlan plan = new WorkoutPlan
            {
                WorkoutPlanRule = new WorkoutPlanRule { Name = "Beginners", Weeks = [new WorkoutPlanRuleWeek(), new WorkoutPlanRuleWeek()] },
                User = new User { FirstName = "Maria", Email = "maria@gym.test" },
                Recordings = [new WorkoutPlanRecording()]
            };

            WorkoutPlanDto dto = _mapper.Map<WorkoutPlanDto>(plan);

            dto.WorkoutPlanRuleName.ShouldBe("Beginners");
            dto.WeekCount.ShouldBe(2);
            dto.User.ShouldNotBeNull().FirstName.ShouldBe("Maria");
            (dto.IsRunning, dto.ElapsedSeconds, dto.HasIncompleteRecording, dto.LastRecordingOn, dto.RecordingCount, dto.LastRecordingDurationSeconds, dto.CurrentWeekMessage)
                .ShouldBe((false, null, false, null, 0, null, ""));

            WorkoutPlanDto without = _mapper.Map<WorkoutPlanDto>(new WorkoutPlan());
            without.WorkoutPlanRuleName.ShouldBe("");
            without.WeekCount.ShouldBe(0);
            without.User.ShouldBeNull();
        }

        // Saving a plan must touch neither the member nor the rule.
        [Fact]
        public void Neither_the_member_nor_the_rule_comes_back_through_a_plan()
        {
            WorkoutPlan plan = _mapper.Map<WorkoutPlan>(new WorkoutPlanDto { UserId = Guid.NewGuid().ToString(), User = new WorkoutPlanOwnerDto { FirstName = "Changed" }, WorkoutPlanRuleId = 3, WorkoutPlanRuleName = "Changed" });

            plan.User.ShouldBeNull();
            plan.WorkoutPlanRule.ShouldBeNull();
            plan.WorkoutPlanRuleId.ShouldBe(3);
            plan.Recordings.ShouldBeEmpty();
        }

        [Fact]
        public void A_rule_counts_its_weeks_and_its_plans()
        {
            WorkoutPlanRuleDto dto = _mapper.Map<WorkoutPlanRuleDto>(new WorkoutPlanRule { Weeks = [new WorkoutPlanRuleWeek { WeekNumber = 2 }], WorkoutPlans = [new WorkoutPlan(), new WorkoutPlan()] });

            dto.WeekCount.ShouldBe(1);
            dto.WorkoutPlanCount.ShouldBe(2);
            dto.IsLocked.ShouldBeFalse();
            dto.Weeks.Single().WeekNumber.ShouldBe(2);
        }

        [Fact]
        public void A_status_is_the_same_status()
        {
            foreach (SubscriptionStatusEnum status in Enum.GetValues<SubscriptionStatusEnum>())
                _mapper.Map<SubscriptionDto>(new Subscription { Status = status }).Status.ShouldBe(status);
        }
    }
}
