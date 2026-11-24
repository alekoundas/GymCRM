using Core.Dtos.User;
using Core.Models;

namespace Core.Tests.Dtos
{
    /// <summary>
    /// A trainer as other people see them. Everything private has to stay behind, so each field of
    /// the answer is checked, not only the ones that are filled.
    /// </summary>
    public sealed class TrainerDtoTests
    {
        private static User Trainer()
        {
            return new User
            {
                Id = new Guid("11111111-2222-3333-4444-555555555555"),
                UserName = "nikos",
                FirstName = "Νίκος",
                LastName = "Παπάς",
                Email = "nikos@gym.test",
                Address = "Athens",
                MedicalHistory = "private",
                ProfileImage = [1, 2, 3],
                UserStatusId = 4,
                UserStatus = new UserStatus { Id = 4, Name = "Active" },
                PhoneNumber = "2101234567"
            };
        }

        private static void ShouldHoldNothingPrivate(UserDto dto)
        {
            dto.Email.ShouldBe("");
            dto.Address.ShouldBe("");
            dto.MedicalHistory.ShouldBe("");
            dto.UserStatusId.ShouldBeNull();
            dto.UserStatus.ShouldBeNull();
            dto.SubscriptionBalance.ShouldBe(0);
            dto.UserRoles.ShouldBeEmpty();
        }

        [Fact]
        public void A_member_sees_a_name_and_a_face()
        {
            UserDto dto = TrainerDto.From(Trainer()).ShouldNotBeNull();

            dto.Id.ShouldBe("11111111-2222-3333-4444-555555555555");
            dto.FirstName.ShouldBe("Νίκος");
            dto.LastName.ShouldBe("Παπάς");
            dto.ProfileImage.ShouldBe([1, 2, 3]);
            dto.UserName.ShouldBe("");
            ShouldHoldNothingPrivate(dto);
        }

        [Fact]
        public void Staff_see_the_username_as_well()
        {
            UserDto dto = TrainerDto.ForStaff(Trainer()).ShouldNotBeNull();

            dto.Id.ShouldBe("11111111-2222-3333-4444-555555555555");
            dto.FirstName.ShouldBe("Νίκος");
            dto.LastName.ShouldBe("Παπάς");
            dto.ProfileImage.ShouldBe([1, 2, 3]);
            dto.UserName.ShouldBe("nikos");
            ShouldHoldNothingPrivate(dto);
        }

        [Fact]
        public void A_trainer_with_no_username_reads_as_an_empty_one()
        {
            User trainer = Trainer();
            trainer.UserName = null;

            TrainerDto.ForStaff(trainer).ShouldNotBeNull().UserName.ShouldBe("");
        }

        [Fact]
        public void A_trainer_with_no_picture_has_none()
        {
            User trainer = Trainer();
            trainer.ProfileImage = null;

            TrainerDto.From(trainer).ShouldNotBeNull().ProfileImage.ShouldBeNull();
        }

        [Fact]
        public void No_trainer_is_nobody()
        {
            TrainerDto.From(null).ShouldBeNull();
            TrainerDto.ForStaff(null).ShouldBeNull();
        }
    }
}
