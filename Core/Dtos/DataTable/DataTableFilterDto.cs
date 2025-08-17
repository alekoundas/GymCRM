namespace Core.Dtos.DataTable
{
    public class DataTableFilterDto
    {
        public DataTableFilterFieldDto? FirstName { get; set; }
        public DataTableFilterFieldDto? LastName { get; set; }
        public DataTableFilterFieldDto? CreatedOn { get; set; }
        public DataTableFilterFieldDto? Value { get; set; }
        public DataTableFilterFieldDto? Id { get; set; }
        public DataTableFilterFieldDto? Description { get; set; }
        public DataTableFilterFieldDto? CustomerId { get; set; }
        public DataTableFilterFieldDto? RoleName { get; set; }
        public DataTableFilterFieldDto? Title { get; set; }

    }
}
