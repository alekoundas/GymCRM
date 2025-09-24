using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Lookup

{
    public class LookupDto
    {
        public int Skip { get; set; }
        public int Take { get; set; }
        public int TotalRecords { get; set; }

        [Required(ErrorMessage = "Filter is required")]
        public LookupFilterDto Filter { get; set; } = null!;

        public List<LookupOptionDto> Data { get; set; } = new List<LookupOptionDto>();
    }
}
