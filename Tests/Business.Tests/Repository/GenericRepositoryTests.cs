using System.Linq.Dynamic.Core.Exceptions;
using Business.Repository;
using Core.Enums;
using Core.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;

namespace Business.Tests.Repository
{
    /// <summary>
    /// The query a repository builds up call by call, and what it does when it is run. Every call is
    /// made on a repository nothing was asked of yet and on one that was, since each has to start the
    /// query itself when it comes first.
    /// </summary>
    public sealed class GenericRepositoryTests : IDisposable
    {
        private static readonly DateTime SomeDay = new DateTime(2031, 3, 5);

        public static readonly TheoryData<bool> FirstCallOrNot = [true, false];

        private readonly TestDatabase _database = new TestDatabase();
        private readonly RecordingFactory _factory;
        private readonly Seed _seed;
        private readonly User _maria;
        private readonly User _eleni;
        private readonly User _nikos;

        public GenericRepositoryTests()
        {
            _factory = new RecordingFactory(_database.Factory);
            _seed = new Seed(_database);

            UserStatus status = _seed.UserStatus(x => x.Name = "Active");
            _maria = _seed.User(x => { x.UserName = "maria"; x.FirstName = "Maria"; x.LastName = "B"; x.UserStatusId = status.Id; x.CreatedOn = SomeDay; });
            _eleni = _seed.User(x => { x.UserName = "eleni"; x.FirstName = "Ελένη"; x.LastName = "A"; x.UserStatusId = status.Id; x.CreatedOn = SomeDay.AddDays(1); });
            _nikos = _seed.User(x => { x.UserName = "nikos"; x.FirstName = "Νίκος"; x.LastName = "B"; x.CreatedOn = SomeDay.AddDays(2); });
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        // One to call first, or one that has been given something to do already.
        private GenericRepository<User> Users(bool isFirstCall = true)
        {
            GenericRepository<User> repository = new GenericRepository<User>(_factory);

            return isFirstCall ? repository : repository.Where(x => x.UserName != "");
        }

        private GenericRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            return new GenericRepository<TEntity>(_factory);
        }

        private static string NamesOf(IEnumerable<User> users)
        {
            return string.Join(" ", users.Select(x => x.UserName));
        }

        private static string SortedNamesOf(IEnumerable<User> users)
        {
            return string.Join(" ", users.Select(x => x.UserName).Order());
        }


        // Related rows.

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public void Include_brings_the_related_row_along(bool isFirstCall)
        {
            User maria = Users(isFirstCall).Include(x => x.UserStatus).ToList(true).Single(x => x.Id == _maria.Id);

            maria.UserStatus.ShouldNotBeNull().Name.ShouldBe("Active");
        }

        [Fact]
        public void Without_it_the_related_row_is_not_there()
        {
            Users().ToList(true).Single(x => x.Id == _maria.Id).UserStatus.ShouldBeNull();
        }

        [Fact]
        public void ThenInclude_goes_on_from_a_list()
        {
            _seed.UserRole(_maria, _seed.Role("Trainer"));

            User maria = Users().Include(x => x.UserRoles).ThenInclude<UserRole, Role>(x => x.Role).ToList(true).Single(x => x.Id == _maria.Id);

            maria.UserRoles.Single().Role.Name.ShouldBe("Trainer");
        }

        [Fact]
        public void ThenInclude_goes_on_from_a_single_row()
        {
            _seed.PhoneNumber(_maria);

            PhoneNumber number = Repository<PhoneNumber>().Include(x => x.User).ThenInclude<User, UserStatus>(x => x.UserStatus!).ToList(true).Single();

            number.User.UserStatus.ShouldNotBeNull().Name.ShouldBe("Active");
        }

        // It has nothing to go on from, so it does nothing at all.
        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public void ThenInclude_on_its_own_is_passed_over(bool isFirstCall)
        {
            List<User> users = Users(isFirstCall).ThenInclude<UserRole, Role>(x => x.Role).ToList(true);

            users.Count.ShouldBe(3);
            users.ShouldAllBe(x => x.UserRoles.Count == 0);
        }

        [Fact]
        public void ThenInclude_from_the_wrong_kind_of_row_is_passed_over()
        {
            List<User> users = Users().Include(x => x.UserStatus).ThenInclude<UserRole, Role>(x => x.Role).ToList(true);

            users.Single(x => x.Id == _maria.Id).UserStatus.ShouldNotBeNull();
        }


        // Other rows than its own.

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public void Select_goes_over_to_the_users_of_the_rows(bool isFirstCall)
        {
            _seed.PhoneNumber(_maria, "1");
            _seed.PhoneNumber(_nikos, "2");
            GenericRepository<PhoneNumber> numbers = Repository<PhoneNumber>();
            if (!isFirstCall)
                numbers.Where(x => x.Number != "");

            SortedNamesOf(numbers.Select<User>(x => x.User).ToList(true)).ShouldBe("maria nikos");
        }

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public void SelectMany_goes_over_to_all_the_users_of_the_rows(bool isFirstCall)
        {
            GenericRepository<UserStatus> statuses = Repository<UserStatus>();
            if (!isFirstCall)
                statuses.Where(x => x.Name != "");

            SortedNamesOf(statuses.SelectMany<User>(x => x.Users).ToList(true)).ShouldBe("eleni maria");
        }


        // The order.

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public void OrderBy_sorts_up(bool isFirstCall)
        {
            NamesOf(Users(isFirstCall).OrderBy(x => x.UserName).ToList()).ShouldBe("eleni maria nikos");
        }

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public void OrderByDescending_sorts_down(bool isFirstCall)
        {
            NamesOf(Users(isFirstCall).OrderByDescending(x => x.UserName).ToList()).ShouldBe("nikos maria eleni");
        }

        [Theory]
        [InlineData(true, OrderDirectionEnum.ASCENDING, "eleni maria nikos")]
        [InlineData(false, OrderDirectionEnum.ASCENDING, "eleni maria nikos")]
        [InlineData(true, OrderDirectionEnum.DESCENDING, "nikos maria eleni")]
        [InlineData(false, OrderDirectionEnum.DESCENDING, "nikos maria eleni")]
        // Anything that is not up is down.
        [InlineData(true, (OrderDirectionEnum)9, "nikos maria eleni")]
        public void OrderBy_a_named_column_sorts_either_way(bool isFirstCall, OrderDirectionEnum direction, string expected)
        {
            NamesOf(Users(isFirstCall).OrderBy("UserName", direction).ToList()).ShouldBe(expected);
        }

        [Theory]
        [InlineData(OrderDirectionEnum.ASCENDING, "eleni maria nikos")]
        [InlineData(OrderDirectionEnum.DESCENDING, "eleni nikos maria")]
        [InlineData((OrderDirectionEnum)9, "eleni nikos maria")]
        public void ThenBy_decides_where_the_first_column_is_equal(OrderDirectionEnum direction, string expected)
        {
            NamesOf(Users().OrderBy("LastName", OrderDirectionEnum.ASCENDING).ThenBy("UserName", direction).ToList()).ShouldBe(expected);
        }

        [Theory]
        [InlineData(OrderDirectionEnum.ASCENDING)]
        [InlineData(OrderDirectionEnum.DESCENDING)]
        public void ThenBy_before_any_order_throws(OrderDirectionEnum direction)
        {
            Should.Throw<InvalidCastException>(() => Users().ThenBy("UserName", direction));
        }

        // A filtered query passes for an ordered one, and is found out a step later.
        [Theory]
        [InlineData(OrderDirectionEnum.ASCENDING)]
        [InlineData(OrderDirectionEnum.DESCENDING)]
        public void ThenBy_after_a_filter_alone_throws_too(OrderDirectionEnum direction)
        {
            Should.Throw<InvalidOperationException>(() => Users(isFirstCall: false).ThenBy("UserName", direction));
        }

        // A later order replaces the one before it.
        [Fact]
        public void The_last_OrderBy_wins()
        {
            NamesOf(Users().OrderBy(x => x.UserName).OrderByDescending(x => x.UserName).ToList()).ShouldBe("nikos maria eleni");
        }


        // Filters.

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public void Where_keeps_the_rows_it_is_true_of(bool isFirstCall)
        {
            NamesOf(Users(isFirstCall).Where(x => x.LastName == "A").ToList()).ShouldBe("eleni");
        }

        [Fact]
        public void One_Where_after_another_keeps_the_rows_both_are_true_of()
        {
            NamesOf(Users().Where(x => x.LastName == "B").Where(x => x.UserStatusId != null).ToList()).ShouldBe("maria");
        }

        [Theory]
        [InlineData(true, "ari", "maria")]
        [InlineData(false, "ari", "maria")]
        [InlineData(true, "i", "eleni maria nikos")]
        [InlineData(true, "", "eleni maria nikos")]
        // Letter for letter, unlike the one below.
        [InlineData(true, "ARI", "")]
        [InlineData(true, "x", "")]
        public void Contains_keeps_the_rows_with_the_text_in_a_column(bool isFirstCall, string value, string expected)
        {
            SortedNamesOf(Users(isFirstCall).Contains("UserName", value).ToList()).ShouldBe(expected);
        }

        [Theory]
        [InlineData(true, null, "ari")]
        [InlineData(true, "", "ari")]
        [InlineData(true, "UserName", null)]
        [InlineData(false, null, null)]
        public void Contains_with_no_column_or_no_text_keeps_every_row(bool isFirstCall, string? column, string? value)
        {
            Users(isFirstCall).Contains(column!, value!).Count().ShouldBe(3);
        }

        [Fact]
        public void Contains_on_a_column_that_is_not_there_throws()
        {
            Should.Throw<ParseException>(() => Users().Contains("Missing", "x"));
        }

        [Theory]
        [InlineData(true, "FirstName", "ελε", "eleni")]
        [InlineData(false, "firstName", "ΕΛΕΝΗ", "eleni")]
        [InlineData(true, "FirstName", "ΝΙΚΟΣ", "nikos")]
        [InlineData(true, "FirstName", "", "eleni maria nikos")]
        [InlineData(true, "FirstName", "x", "")]
        [InlineData(true, "UserStatus.Name", "ACT", "eleni maria")]
        // No status reads as an empty text.
        [InlineData(true, "UserStatus.Name", "", "eleni maria nikos")]
        public void FilterByColumnContains_ignores_case_and_accents(bool isFirstCall, string column, string value, string expected)
        {
            SortedNamesOf(Users(isFirstCall).FilterByColumnContains(column, value).ToList()).ShouldBe(expected);
        }

        [Theory]
        [InlineData(true, "LastName", "B", "maria nikos")]
        [InlineData(false, "lastName", "B", "maria nikos")]
        [InlineData(true, "LastName", "b", "")]
        [InlineData(true, "UserStatusId", null, "nikos")]
        [InlineData(true, "UserStatusId", "", "nikos")]
        [InlineData(true, "UserStatusId", "999", "")]
        [InlineData(true, "CreatedOn", "2031-03-05", "maria")]
        public void FilterByColumnEquals_keeps_the_rows_with_the_value(bool isFirstCall, string column, string? value, string expected)
        {
            SortedNamesOf(Users(isFirstCall).FilterByColumnEquals(column, value).ToList()).ShouldBe(expected);
        }

        [Fact]
        public void FilterByColumnEquals_finds_one_row_by_its_id()
        {
            NamesOf(Users().FilterByColumnEquals("Id", _nikos.Id).ToList()).ShouldBe("nikos");
        }

        [Theory]
        [InlineData(true, "LastName", "B", "eleni")]
        [InlineData(false, "lastName", "B", "eleni")]
        [InlineData(true, "UserStatusId", null, "eleni maria")]
        [InlineData(true, "UserStatusId", "999", "eleni maria nikos")]
        public void FilterByColumnNotEquals_keeps_every_other_row(bool isFirstCall, string column, string? value, string expected)
        {
            SortedNamesOf(Users(isFirstCall).FilterByColumnNotEquals(column, value).ToList()).ShouldBe(expected);
        }

        [Theory]
        [InlineData(true, "eleni nikos", "eleni nikos")]
        [InlineData(false, "eleni nikos", "eleni nikos")]
        [InlineData(true, "maria nobody", "maria")]
        [InlineData(true, "nobody", "")]
        [InlineData(true, "", "")]
        public void FilterByColumnIn_keeps_the_rows_with_any_of_the_values(bool isFirstCall, string values, string expected)
        {
            string[] list = values.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            SortedNamesOf(Users(isFirstCall).FilterByColumnIn("UserName", list).ToList()).ShouldBe(expected);
        }

        [Fact]
        public void FilterByColumnIn_takes_ids_as_text_and_empty_values()
        {
            SortedNamesOf(Users().FilterByColumnIn("Id", new[] { _maria.Id.ToString(), _nikos.Id.ToString() }).ToList()).ShouldBe("maria nikos");
            SortedNamesOf(Users().FilterByColumnIn("UserStatusId", new string?[] { null }).ToList()).ShouldBe("nikos");
        }

        [Theory]
        [InlineData(true, 0, 1, "eleni maria")]
        [InlineData(false, 0, 1, "eleni maria")]
        [InlineData(true, 1, 1, "eleni")]
        [InlineData(true, 1, 9, "eleni nikos")]
        [InlineData(true, 3, 9, "")]
        public void FilterByColumnDateBetween_keeps_the_rows_inside(bool isFirstCall, int from, int to, string expected)
        {
            SortedNamesOf(Users(isFirstCall).FilterByColumnDateBetween("CreatedOn", SomeDay.AddDays(from), SomeDay.AddDays(to)).ToList()).ShouldBe(expected);
        }


        // Pages.

        [Theory]
        [InlineData(true, 0, 2, "eleni maria")]
        [InlineData(false, 0, 2, "eleni maria")]
        [InlineData(true, 1, 1, "maria")]
        [InlineData(true, 2, 2, "nikos")]
        [InlineData(true, 3, 2, "")]
        [InlineData(true, 0, 0, "")]
        [InlineData(true, 0, 100, "eleni maria nikos")]
        // A page that makes no sense is every row.
        [InlineData(true, -1, 2, "eleni maria nikos")]
        [InlineData(true, 0, -1, "eleni maria nikos")]
        [InlineData(true, -1, -1, "eleni maria nikos")]
        public void AddPagging_skips_and_takes(bool isFirstCall, int skip, int take, string expected)
        {
            GenericRepository<User> users = Users(isFirstCall).OrderBy(x => x.UserName).AddPagging(skip, take);

            NamesOf(users.ToList()).ShouldBe(expected);
        }

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public void AddPagging_works_as_the_first_call_too(bool isFirstCall)
        {
            Users(isFirstCall).AddPagging(0, 2).Count().ShouldBe(2);
        }

        // Told nothing it skips ten and takes one.
        [Fact]
        public void AddPagging_told_nothing_takes_the_eleventh_row()
        {
            for (int i = 0; i < 8; i++)
                _seed.User();

            Users().AddPagging().Count().ShouldBe(1);
            Users().Where(x => x.UserName!.StartsWith("user")).AddPagging().Count().ShouldBe(0);
        }


        // Whether there are any, and how many.

        [Theory]
        [InlineData(true, "maria", true)]
        [InlineData(false, "maria", true)]
        [InlineData(true, "nobody", false)]
        [InlineData(false, "nobody", false)]
        public async Task Any_says_whether_a_row_is_there(bool isFirstCall, string userName, bool expected)
        {
            Users(isFirstCall).Any(x => x.UserName == userName).ShouldBe(expected);
            (await Users(isFirstCall).AnyAsync(x => x.UserName == userName)).ShouldBe(expected);
        }

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public async Task Count_counts_what_is_left(bool isFirstCall)
        {
            Users(isFirstCall).Count().ShouldBe(3);
            (await Users(isFirstCall).CountAsync()).ShouldBe(3);

            Users(isFirstCall).Where(x => x.LastName == "B").Count().ShouldBe(2);
            (await Users(isFirstCall).Where(x => x.LastName == "B").CountAsync()).ShouldBe(2);
        }


        // Adding.

        private static PhoneNumber Number(User user, string number)
        {
            return new PhoneNumber { UserId = user.Id, Number = number };
        }

        private string NumbersInDatabase()
        {
            return _database.Read(db => string.Join(" ", db.PhoneNumbers.OrderBy(x => x.Number).Select(x => x.Number)));
        }

        [Fact]
        public async Task Add_writes_the_row_gives_it_an_id_and_says_how_many_it_wrote()
        {
            PhoneNumber first = Number(_maria, "1");
            PhoneNumber second = Number(_maria, "2");

            Repository<PhoneNumber>().Add(first).ShouldBe(1);
            (await Repository<PhoneNumber>().AddAsync(second)).ShouldBe(1);

            first.Id.ShouldBeGreaterThan(0);
            second.Id.ShouldBeGreaterThan(first.Id);
            NumbersInDatabase().ShouldBe("1 2");
        }

        [Fact]
        public async Task Add_counts_the_rows_that_hang_off_it()
        {
            WorkoutPlanRule first = new WorkoutPlanRule { Name = "one", Weeks = [new WorkoutPlanRuleWeek { WeekNumber = 1 }, new WorkoutPlanRuleWeek { WeekNumber = 2 }] };
            WorkoutPlanRule second = new WorkoutPlanRule { Name = "two", Weeks = [new WorkoutPlanRuleWeek { WeekNumber = 1 }] };

            Repository<WorkoutPlanRule>().Add(first).ShouldBe(3);
            (await Repository<WorkoutPlanRule>().AddAsync(second)).ShouldBe(2);
        }

        [Fact]
        public async Task AddRange_writes_them_all()
        {
            Repository<PhoneNumber>().AddRange([Number(_maria, "1"), Number(_maria, "2")]).ShouldBe(2);
            (await Repository<PhoneNumber>().AddRangeAsync([Number(_maria, "3"), Number(_maria, "4"), Number(_maria, "5")])).ShouldBe(3);

            NumbersInDatabase().ShouldBe("1 2 3 4 5");
        }

        [Fact]
        public async Task AddRange_of_none_writes_none()
        {
            Repository<PhoneNumber>().AddRange([]).ShouldBe(0);
            (await Repository<PhoneNumber>().AddRangeAsync([])).ShouldBe(0);
        }

        [Fact]
        public async Task AddRange_writes_all_of_them_or_none()
        {
            PhoneNumber nobodys = new PhoneNumber { UserId = Guid.NewGuid(), Number = "2" };

            Should.Throw<DbUpdateException>(() => Repository<PhoneNumber>().AddRange([Number(_maria, "1"), nobodys]));
            await Should.ThrowAsync<DbUpdateException>(() => Repository<PhoneNumber>().AddRangeAsync([Number(_maria, "1"), nobodys]));

            NumbersInDatabase().ShouldBe("");
        }

        [Fact]
        public async Task A_row_the_database_refuses_throws()
        {
            Should.Throw<DbUpdateException>(() => Repository<PhoneNumber>().Add(new PhoneNumber { UserId = Guid.NewGuid() }));
            await Should.ThrowAsync<DbUpdateException>(() => Repository<PhoneNumber>().AddAsync(new PhoneNumber { UserId = Guid.NewGuid() }));
        }


        // Removing.

        [Fact]
        public async Task Remove_deletes_the_row()
        {
            PhoneNumber first = _seed.PhoneNumber(_maria, "1");
            PhoneNumber second = _seed.PhoneNumber(_maria, "2");
            _seed.PhoneNumber(_maria, "3");

            Repository<PhoneNumber>().Remove(first).ShouldBe(1);
            (await Repository<PhoneNumber>().RemoveAsync(second)).ShouldBe(1);

            NumbersInDatabase().ShouldBe("3");
        }

        // Found by its id alone.
        [Fact]
        public void Remove_needs_no_more_than_the_id()
        {
            PhoneNumber number = _seed.PhoneNumber(_maria, "1");

            Repository<PhoneNumber>().Remove(new PhoneNumber { Id = number.Id }).ShouldBe(1);

            NumbersInDatabase().ShouldBe("");
        }

        [Fact]
        public async Task Remove_of_a_row_that_is_gone_throws()
        {
            Should.Throw<DbUpdateConcurrencyException>(() => Repository<PhoneNumber>().Remove(new PhoneNumber { Id = 99 }));
            await Should.ThrowAsync<DbUpdateConcurrencyException>(() => Repository<PhoneNumber>().RemoveAsync(new PhoneNumber { Id = 99 }));
        }

        [Fact]
        public async Task RemoveRange_deletes_them_all()
        {
            PhoneNumber[] numbers = Enumerable.Range(1, 5).Select(x => _seed.PhoneNumber(_maria, x.ToString())).ToArray();

            Repository<PhoneNumber>().RemoveRange(numbers[..2]).ShouldBe(2);
            (await Repository<PhoneNumber>().RemoveRangeAsync(numbers[2..4])).ShouldBe(2);

            NumbersInDatabase().ShouldBe("5");
        }

        [Fact]
        public async Task RemoveRange_of_none_deletes_none()
        {
            _seed.PhoneNumber(_maria, "1");

            Repository<PhoneNumber>().RemoveRange([]).ShouldBe(0);
            (await Repository<PhoneNumber>().RemoveRangeAsync([])).ShouldBe(0);

            NumbersInDatabase().ShouldBe("1");
        }


        // Finding one.

        [Fact]
        public async Task Find_gives_the_row_with_the_id()
        {
            Users().Find(_maria.Id).ShouldNotBeNull().UserName.ShouldBe("maria");
            (await Users().FindAsync(_maria.Id)).ShouldNotBeNull().UserName.ShouldBe("maria");

            PhoneNumber number = _seed.PhoneNumber(_maria, "1");
            Repository<PhoneNumber>().Find(number.Id).ShouldNotBeNull().Number.ShouldBe("1");
            (await Repository<PhoneNumber>().FindAsync(number.Id)).ShouldNotBeNull().Number.ShouldBe("1");
        }

        [Fact]
        public async Task Find_gives_nothing_for_an_id_nobody_has()
        {
            Users().Find(Guid.NewGuid()).ShouldBeNull();
            (await Users().FindAsync(Guid.NewGuid())).ShouldBeNull();
            Repository<PhoneNumber>().Find(99).ShouldBeNull();
        }

        [Fact]
        public async Task Find_gives_nothing_for_no_id()
        {
            Users().Find(null).ShouldBeNull();
            (await Users().FindAsync(null)).ShouldBeNull();
        }

        // It looks past whatever was asked for before it.
        [Fact]
        public async Task Find_takes_no_notice_of_a_filter()
        {
            Users().Where(x => x.UserName == "nikos").Find(_maria.Id).ShouldNotBeNull();
            (await Users().Where(x => x.UserName == "nikos").FindAsync(_maria.Id)).ShouldNotBeNull();
        }

        // An id in the wrong type is not turned into the right one.
        [Fact]
        public async Task Find_with_an_id_of_the_wrong_type_throws()
        {
            Should.Throw<ArgumentException>(() => Users().Find(_maria.Id.ToString()));
            await Should.ThrowAsync<ArgumentException>(() => Users().FindAsync(_maria.Id.ToString()));
        }

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public async Task FirstAsync_gives_the_first_row_it_is_true_of(bool isFirstCall)
        {
            (await Users(isFirstCall).OrderByDescending(x => x.UserName).FirstAsync(x => x.LastName == "B")).UserName.ShouldBe("nikos");
        }

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public async Task FirstAsync_works_as_the_first_call_too(bool isFirstCall)
        {
            (await Users(isFirstCall).FirstAsync(x => x.UserName == "maria")).Id.ShouldBe(_maria.Id);
        }

        [Fact]
        public async Task FirstAsync_throws_when_it_is_true_of_none()
        {
            await Should.ThrowAsync<InvalidOperationException>(() => Users().FirstAsync(x => x.UserName == "nobody"));
        }

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public async Task FirstOrDefault_gives_the_first_row_it_is_true_of(bool isFirstCall)
        {
            Users(isFirstCall).FirstOrDefault(x => x.UserName == "maria").ShouldNotBeNull().Id.ShouldBe(_maria.Id);
            (await Users(isFirstCall).FirstOrDefaultAsync(x => x.UserName == "maria")).ShouldNotBeNull().Id.ShouldBe(_maria.Id);
        }

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public async Task FirstOrDefault_gives_nothing_when_it_is_true_of_none(bool isFirstCall)
        {
            Users(isFirstCall).FirstOrDefault(x => x.UserName == "nobody").ShouldBeNull();
            (await Users(isFirstCall).FirstOrDefaultAsync(x => x.UserName == "nobody")).ShouldBeNull();
        }

        [Fact]
        public async Task FirstOrDefault_told_nothing_gives_the_first_row_there_is()
        {
            Users().OrderBy(x => x.UserName).FirstOrDefault().ShouldNotBeNull().UserName.ShouldBe("eleni");
            (await Users().OrderBy(x => x.UserName).FirstOrDefaultAsync()).ShouldNotBeNull().UserName.ShouldBe("eleni");

            Repository<PhoneNumber>().FirstOrDefault().ShouldBeNull();
            (await Repository<PhoneNumber>().FirstOrDefaultAsync()).ShouldBeNull();
        }


        // All of them.

        [Theory]
        [MemberData(nameof(FirstCallOrNot))]
        public async Task ToList_gives_every_row_left(bool isFirstCall)
        {
            SortedNamesOf(Users(isFirstCall).ToList()).ShouldBe("eleni maria nikos");
            SortedNamesOf(Users(isFirstCall).ToList(true)).ShouldBe("eleni maria nikos");
            SortedNamesOf(await Users(isFirstCall).ToListAsync()).ShouldBe("eleni maria nikos");
            SortedNamesOf(await Users(isFirstCall).ToListAsync(true)).ShouldBe("eleni maria nikos");
        }

        [Fact]
        public async Task ToList_gives_an_empty_list_when_none_are_left()
        {
            Users().Where(x => x.UserName == "nobody").ToList().ShouldBeEmpty();
            (await Users().Where(x => x.UserName == "nobody").ToListAsync()).ShouldBeEmpty();
        }

        // The flag means one thing in the one and the opposite in the other.
        [Theory]
        [InlineData(false, 3)]
        [InlineData(true, 0)]
        public void ToList_keeps_track_of_its_rows_unless_tracking_is_enabled(bool isTrackingEnabled, int tracked)
        {
            Users().ToList(isTrackingEnabled);

            _factory.Last.ChangeTracker.Entries().Count().ShouldBe(tracked);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 3)]
        public async Task ToListAsync_keeps_track_of_its_rows_only_if_tracking_is_enabled(bool isTrackingEnabled, int tracked)
        {
            await Users().ToListAsync(isTrackingEnabled);

            _factory.Last.ChangeTracker.Entries().Count().ShouldBe(tracked);
        }

        [Fact]
        public async Task A_single_row_is_never_kept_track_of()
        {
            Users().FirstOrDefault();
            _factory.Last.ChangeTracker.Entries().ShouldBeEmpty();

            Users().FirstOrDefault(x => x.UserName == "maria");
            _factory.Last.ChangeTracker.Entries().ShouldBeEmpty();

            await Users().FirstOrDefaultAsync();
            _factory.Last.ChangeTracker.Entries().ShouldBeEmpty();

            await Users().FirstOrDefaultAsync(x => x.UserName == "maria");
            _factory.Last.ChangeTracker.Entries().ShouldBeEmpty();

            await Users().FirstAsync(x => x.UserName == "maria");
            _factory.Last.ChangeTracker.Entries().ShouldBeEmpty();
        }


        // One repository, one query after another.

        public static TheoryData<string> EveryEnd
        {
            get
            {
                return
                [
                    "Any", "AnyAsync", "Count", "CountAsync", "Find", "FindAsync", "FirstAsync", "FirstOrDefault", "FirstOrDefaultAsync",
                    "FirstOrDefault told nothing", "FirstOrDefaultAsync told nothing", "ToList", "ToListAsync", "Add", "AddAsync",
                    "AddRange", "AddRangeAsync", "Remove", "RemoveAsync", "RemoveRange", "RemoveRangeAsync", "Dispose"
                ];
            }
        }

        // Whatever ends a query leaves nothing of it for the next one.
        [Theory]
        [MemberData(nameof(EveryEnd))]
        public async Task A_finished_query_leaves_nothing_behind(string end)
        {
            PhoneNumber first = _seed.PhoneNumber(_maria, "1");
            GenericRepository<PhoneNumber> numbers = Repository<PhoneNumber>();
            numbers.Where(x => x.Number == "1");

            switch (end)
            {
                case "Any":
                    numbers.Any(x => true);
                    break;
                case "AnyAsync":
                    await numbers.AnyAsync(x => true);
                    break;
                case "Count":
                    numbers.Count();
                    break;
                case "CountAsync":
                    await numbers.CountAsync();
                    break;
                case "Find":
                    numbers.Find(first.Id);
                    break;
                case "FindAsync":
                    await numbers.FindAsync(first.Id);
                    break;
                case "FirstAsync":
                    await numbers.FirstAsync(x => true);
                    break;
                case "FirstOrDefault":
                    numbers.FirstOrDefault(x => true);
                    break;
                case "FirstOrDefaultAsync":
                    await numbers.FirstOrDefaultAsync(x => true);
                    break;
                case "FirstOrDefault told nothing":
                    numbers.FirstOrDefault();
                    break;
                case "FirstOrDefaultAsync told nothing":
                    await numbers.FirstOrDefaultAsync();
                    break;
                case "ToList":
                    numbers.ToList();
                    break;
                case "ToListAsync":
                    await numbers.ToListAsync();
                    break;
                case "Add":
                    numbers.Add(Number(_maria, "x"));
                    break;
                case "AddAsync":
                    await numbers.AddAsync(Number(_maria, "x"));
                    break;
                case "AddRange":
                    numbers.AddRange([]);
                    break;
                case "AddRangeAsync":
                    await numbers.AddRangeAsync([]);
                    break;
                case "Remove":
                    numbers.Remove(first);
                    break;
                case "RemoveAsync":
                    await numbers.RemoveAsync(first);
                    break;
                case "RemoveRange":
                    numbers.RemoveRange([]);
                    break;
                case "RemoveRangeAsync":
                    await numbers.RemoveRangeAsync([]);
                    break;
                default:
                    numbers.Dispose();
                    break;
            }

            _database.Change(db => db.PhoneNumbers.ExecuteDelete());
            _seed.PhoneNumber(_maria, "2");
            _seed.PhoneNumber(_maria, "3");

            numbers.Count().ShouldBe(2);
        }

        // One that failed is still there, and the next call goes on from it.
        [Fact]
        public async Task A_query_that_failed_is_still_there_for_the_next_one()
        {
            GenericRepository<User> users = Users().Where(x => x.UserName == "nobody");

            await Should.ThrowAsync<InvalidOperationException>(() => users.FirstAsync(x => true));

            users.Count().ShouldBe(0);
            users.Count().ShouldBe(3);
        }

        [Fact]
        public void A_whole_query_runs_on_one_context()
        {
            Users().Include(x => x.UserStatus).Where(x => x.LastName == "B").OrderBy("UserName", OrderDirectionEnum.ASCENDING).AddPagging(0, 5).ToList();

            _factory.Count.ShouldBe(1);
        }

        // The contexts of the queries are never closed by it; the ones it writes with are.
        [Fact]
        public async Task What_it_writes_with_is_closed_again()
        {
            PhoneNumber number = Number(_maria, "1");

            Repository<PhoneNumber>().Add(number);
            ShouldBeClosed(_factory.Last);
            Repository<PhoneNumber>().Remove(number);
            ShouldBeClosed(_factory.Last);

            PhoneNumber other = Number(_maria, "2");
            await Repository<PhoneNumber>().AddAsync(other);
            ShouldBeClosed(_factory.Last);
            await Repository<PhoneNumber>().RemoveAsync(other);
            ShouldBeClosed(_factory.Last);

            Repository<PhoneNumber>().AddRange([]);
            ShouldBeClosed(_factory.Last);
            Repository<PhoneNumber>().RemoveRange([]);
            ShouldBeClosed(_factory.Last);

            await Repository<PhoneNumber>().AddRangeAsync([]);
            ShouldBeClosed(_factory.Last);
            await Repository<PhoneNumber>().RemoveRangeAsync([]);
            ShouldBeClosed(_factory.Last);
        }

        private static void ShouldBeClosed(ApiDbContext db)
        {
            Should.Throw<ObjectDisposedException>(() => db.Users.Count());
        }

        private sealed class RecordingFactory : IDbContextFactory<ApiDbContext>
        {
            private readonly IDbContextFactory<ApiDbContext> _inner;

            public RecordingFactory(IDbContextFactory<ApiDbContext> inner)
            {
                _inner = inner;
            }

            public ApiDbContext Last { get; private set; } = null!;

            public int Count { get; private set; }

            public ApiDbContext CreateDbContext()
            {
                Count++;
                Last = _inner.CreateDbContext();

                return Last;
            }
        }
    }
}
