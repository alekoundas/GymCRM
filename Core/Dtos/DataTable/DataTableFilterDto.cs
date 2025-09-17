using Core.Enums;

namespace Core.Dtos.DataTable
{
    public class DataTableFilterDto
    {
        public string FieldName { get; set; } = "";
        public string? Value { get; set; }

        public DataTableFiltersEnum FilterType { get; set; } = DataTableFiltersEnum.contains;

    }
}
