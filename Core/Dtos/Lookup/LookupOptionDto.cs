namespace Core.Dtos.Lookup
{
    public  class LookupOptionDto
    {
        public string? Id { get; set; } = "";
        public string? Value { get; set; } = "";
        public string? FirstName { get; set; } = "";// TODO: Refactor all the lookup logic
        public string? LastName { get; set; } = ""; // TODO: Refactor all the lookup logic
        public byte[]? ProfileImage { get; set; }

    }
}
