using Core.Enums;

namespace Core.Dtos.DataTable
{
    public class DataTableFilterDto
    {
        public string FieldName { get; set; } = "";
        public string? Value { get; set; }
        public List<string> Values { get; set; } = new List<string>();

        public DataTableFiltersEnum FilterType { get; set; } = DataTableFiltersEnum.contains;

    }
}
