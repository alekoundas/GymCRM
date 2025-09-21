

namespace Core.Dtos.AutoComplete
{
    public class AutoCompleteDto<TEntity>
    {
        public string SearchValue { get; set; } = "";
        public int Skip { get; set; }
        public int Take { get; set; }
        public int TotalRecords { get; set; }
        public List<TEntity> Suggestions { get; set; } = new List<TEntity>();
    }
}
