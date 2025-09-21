using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public class GenericController<TEntity, TEntityDto, TEntityAddDto> : ControllerBase
        where TEntity : class
        where TEntityDto : class
        where TEntityAddDto : class
    {
        private readonly IDataService _dataService;
        //private readonly ILogger<TEntityController> _logger;
        private readonly IMapper _mapper;
        public GenericController(IDataService dataService, IMapper mapper)//, ILogger<TEntityController> logger)
        {
            //_logger = logger;
            _dataService = dataService;
            _mapper = mapper;
        }


        // GET: api/controller/5
        [HttpGet("{id}")]
        public virtual async Task<ActionResult<ApiResponse<TEntityDto>>> Get(int id)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<TEntityDto>().SetErrorResponse("error", "User is not authorized to perform this action.");


            TEntity? entity = await _dataService.GetGenericRepository<TEntity>().FindAsync(id);
            TEntityDto entityDto = _mapper.Map<TEntityDto>(entity);
            if (entityDto == null)
            {
                string className = typeof(TEntity).Name;
                return new ApiResponse<TEntityDto>().SetErrorResponse("error", $"Requested {className} not found!");
            }

            return new ApiResponse<TEntityDto>().SetSuccessResponse(entityDto);
        }

        // POST: api/controller
        [HttpPost]
        public virtual async Task<ActionResult<ApiResponse<List<TEntity>>>> Post([FromBody] List<TEntityAddDto> entityDtos)
        {
            if (!IsUserAuthorized("Add"))
                return new ApiResponse<List<TEntity>>().SetErrorResponse("error", "User is not authorized to perform this action.");

            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<List<TEntity>>().SetErrorResponse("error", "Invalid data provided."));

            foreach (var entityDto in entityDtos)
                if (CustomValidatePOST(entityDto, out string[] errors))
                    return BadRequest(new ApiResponse<TEntity>().SetErrorResponse("error", errors));

            List<TEntity> entities = _mapper.Map<List<TEntity>>(entityDtos);

            int result = await _dataService.GetGenericRepository<TEntity>().AddRangeAsync(entities);
            if (result <= 0)
                return new ApiResponse<List<TEntity>>().SetErrorResponse("error", "An error occurred while creating the entity.");

            return new ApiResponse<List<TEntity>>().SetSuccessResponse(entities);
        }

        // PUT: api/controller/5
        [HttpPut("{id}")]
        public virtual async Task<ActionResult<ApiResponse<TEntity>>> Put(int id, [FromBody] TEntityDto entityDto)
        {
            if (!IsUserAuthorized("Edit"))
                return new ApiResponse<TEntity>().SetErrorResponse("error", "User is not authorized to perform this action.");

            if (!ModelState.IsValid)
                return new ApiResponse<TEntity>().SetErrorResponse("error", "Invalid data provided.");

            if (CustomValidatePUT(entityDto, out string[] errors))
                return BadRequest(new ApiResponse<TEntity>().SetErrorResponse("error", errors));

            TEntity entity = _mapper.Map<TEntity>(entityDto);


            TEntity? existingEntity = await _dataService.GetGenericRepository<TEntity>().FindAsync(id);
            if (existingEntity == null)
            {
                string className = typeof(TEntity).Name;
                return new ApiResponse<TEntity>().SetErrorResponse("error", $"Requested {className} not found!");
            }

            _dataService.Update(entity);
            return new ApiResponse<TEntity>().SetSuccessResponse(entity);
        }

        // DELETE: api/controller/5
        [HttpDelete("{id}")]
        public virtual async Task<ActionResult<ApiResponse<TEntity>>> Delete(int id)
        {
            if (!IsUserAuthorized("Delete"))
                return new ApiResponse<TEntity>().SetErrorResponse("error", "You dont have the permitions to request this information.");


            TEntity? entity = await _dataService.GetGenericRepository<TEntity>().FindAsync(id);
            if (entity == null)
            {
                string className = typeof(TEntity).Name;
                return new ApiResponse<TEntity>().SetErrorResponse("error", $"Requested {className} not found!");
            }

            int result = await _dataService.GetGenericRepository<TEntity>().RemoveAsync(entity);
            if (result != 1)
                return new ApiResponse<TEntity>().SetErrorResponse("error", "An error occurred while deleting the entity.");

            return new ApiResponse<TEntity>().SetSuccessResponse(entity);
        }






        // POST: api/controller/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<TEntityDto>>> GetDataTable([FromBody] DataTableDto<TEntityDto> dataTable)
        {
            var query = _dataService.GetGenericRepository<TEntity>();

            // Handle Sorting of DataTable.
            if (dataTable.Sorts.Count() > 0)
            {
                // Create the first OrderBy().
                DataTableSortDto? dataTableSort = dataTable.Sorts.First();
                if (dataTableSort.Order > 0)
                    query.OrderBy(dataTableSort.FieldName.Substring(0, 1).ToUpper() + dataTableSort.FieldName.Substring(1, dataTableSort.FieldName.Length), OrderDirectionEnum.ASCENDING);
                else if (dataTableSort.Order < 0)
                    query.OrderBy(dataTableSort.FieldName.Substring(0, 1).ToUpper() + dataTableSort.FieldName.Substring(1, dataTableSort.FieldName.Length), OrderDirectionEnum.DESCENDING);

                // Create the rest OrderBy methods as ThenBy() if any.
                foreach (var sortInfo in dataTable.Sorts.Skip(1))
                {
                    if (dataTableSort.Order > 0)
                        query.ThenBy(sortInfo.FieldName.Substring(0, 1).ToUpper() + sortInfo.FieldName.Substring(1, sortInfo.FieldName.Length), OrderDirectionEnum.ASCENDING);
                    else if (dataTableSort.Order < 0)
                        query.ThenBy(sortInfo.FieldName.Substring(0, 1).ToUpper() + sortInfo.FieldName.Substring(1, sortInfo.FieldName.Length), OrderDirectionEnum.DESCENDING);
                }
            }

            // Handle Filtering of DataTable.
            foreach (var filter in dataTable.Filters.Where(x => x.FilterType == DataTableFiltersEnum.equals))
            {
                if (filter.FieldName == "UserId" && filter.Value != null)
                    query.FilterByColumnEquals(filter.FieldName, new Guid(filter.Value));
                else
                    query.FilterByColumnEquals(filter.FieldName, filter.Value);
            }

            foreach (var filter in dataTable.Filters.Where(x => x.FilterType == DataTableFiltersEnum.contains))
                if (filter.Value != null)
                    query.FilterByColumnContains(filter.FieldName, filter.Value);

            foreach (var filter in dataTable.Filters.Where(x => x.FilterType == DataTableFiltersEnum.notEquals))
                query.FilterByColumnNotEquals(filter.FieldName, filter.Value);

            
            // Retrieve record count (with filters, no paging).
            dataTable.PageCount = await query.CountAsync();

            // Handle Pagging of DataTable.
            int skip = (dataTable.Page - 1) * dataTable.Rows;
            int take = dataTable.Rows;
            query.AddPagging(skip, take);



            // Retrieve Data.
            List<TEntity> result = await query.ToListAsync();
            List<TEntityDto> customerDto = _mapper.Map<List<TEntityDto>>(result);

            //TODO add filter
            int rows = await _dataService
                .GetGenericRepository<TEntity>()
                //.Where()
                .CountAsync();

            dataTable.Data = customerDto;
            dataTable.PageCount = rows;

            return new ApiResponse<DataTableDto<TEntityDto>>().SetSuccessResponse(dataTable);
        }



        protected virtual bool CustomValidatePOST(TEntityAddDto entity, out string[] errors)
        {
            errors = Array.Empty<string>();
            return false;
        }
        protected virtual bool CustomValidatePUT(TEntityDto entity, out string[] errors)
        {
            errors = Array.Empty<string>();
            return false;
        }

        protected virtual bool IsUserAuthorized(string action)
        {
            string controllerName = ControllerContext.ActionDescriptor.ControllerName;
            string claimName = controllerName + "_" + action;
            bool hasClaim = User.HasClaim("Permission", claimName);
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return hasClaim;
        }
    }
}