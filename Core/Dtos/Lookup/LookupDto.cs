namespace Core.Dtos.Lookup

{
    public class LookupDto
    {
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public LookupFilterDto? Filter { get; set; }

        public List<LookupOptionDto>? Data { get; set; } = new List<LookupOptionDto>();
    }
}
