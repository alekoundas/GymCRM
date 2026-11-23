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
    }
}
