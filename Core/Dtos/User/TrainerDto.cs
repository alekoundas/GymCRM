namespace Core.Dtos.User
{
    public static class TrainerDto
    {
        // A trainer as members see them next to a session: a name and a face. Members
        // used to get the trainer's whole record with every session - email, address,
        // medical history and all - because the full user was mapped across.
        public static UserDto? From(Models.User? trainer)
        {
            if (trainer == null)
                return null;

            return new UserDto
            {
                Id = trainer.Id.ToString(),
                FirstName = trainer.FirstName,
                LastName = trainer.LastName,
                ProfileImage = trainer.ProfileImage
            };
        }

        // The same for staff screens, which show everyone as "First Last (username)".
        // Still nothing private: staff can already read usernames in every member picker.
        public static UserDto? ForStaff(Models.User? trainer)
        {
            UserDto? trainerDto = From(trainer);
            if (trainerDto != null)
                trainerDto.UserName = trainer!.UserName ?? "";

            return trainerDto;
        }
    }
}
