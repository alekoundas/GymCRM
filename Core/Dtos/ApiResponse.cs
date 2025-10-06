namespace Core.Dtos
{
    public class ApiResponse<TEntity>
    {
        public bool IsSucceed { get; private set; } = true;
        public Dictionary<string, string[]> Messages { get; private set; } = [];
        public TEntity? Data { get; private set; }


        public ApiResponse<TEntity> SetSuccessResponse(TEntity data)
        {
            Data = data;
            return this;
        }
        public ApiResponse<TEntity> SetSuccessResponse(string value)
        {
            Messages.Add("success", [value]);
            return this;
        }
        public ApiResponse<TEntity> SetSuccessResponse(TEntity data, string value)
        {
            Data = data;
            Messages.Add("success", [value]);
            return this;
        }
        public ApiResponse<TEntity> SetSuccessResponse(TEntity data, string[] value)
        {
            Data = data;
            Messages.Add("success", value);
            return this;
        }

        public ApiResponse<TEntity> SetSuccessResponse(TEntity data, Dictionary<string, string[]> message)
        {
            Data = data;
            Messages = message;
            return this;
        }


        public ApiResponse<TEntity> SetErrorResponse(string value)
        {
            IsSucceed = false;
            Messages.Add("error", [value]);
            return this;
        }
        public ApiResponse<TEntity> SetErrorResponse(string[] value)
        {
            IsSucceed = false;
            Messages.Add("error", value);
            return this;
        }
        public ApiResponse<TEntity> SetErrorResponse(Dictionary<string, string[]> message)
        {
            IsSucceed = false;
            Messages = message;
            return this;
        }
    }
}
