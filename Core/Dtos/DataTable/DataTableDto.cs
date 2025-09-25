namespace Core.Dtos.DataTable
{
    public class DataTableDto<TEntity>
    {
        public IEnumerable<TEntity>? Data { get; set; }
        public int Rows { get; set; }
        public int Page { get; set; }
        public int PageCount { get; set; }
        public int TotalRecords { get; set; }
        public IEnumerable<DataTableSortDto> Sorts { get; set; } = new List<DataTableSortDto>();
        public IEnumerable<DataTableFilterDto> Filters { get; set; } = new List<DataTableFilterDto>();

    }
}
