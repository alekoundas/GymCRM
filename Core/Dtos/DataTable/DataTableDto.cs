namespace Core.Dtos.DataTable
{
    public class DataTableDto<TEntity>
    {
        public IEnumerable<TEntity>? Data { get; set; }
        public int Rows { get; set; }
        public int Page { get; set; }
        public int PageCount { get; set; }
        public IEnumerable<DataTableSortDto>? MultiSortMeta { get; set; } = new List<DataTableSortDto>();
        public DataTableFilterDto? Filters { get; set; }

    }
}
