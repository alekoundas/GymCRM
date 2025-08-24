using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Enums;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenericController<TEntity, TEntityDto> : ControllerBase where TEntity : class
    {
        private readonly IDataService _dataService;
        //private readonly ILogger<TEntityController> _logger;
        private readonly IMapper _mapper;
        public GenericController(IDataService dataService, IMapper mapper)//, ILogger<TEntityController> logger)
        {
            //_logger = logger;
            _dataService = dataService;
            _mapper= mapper;
        }


        // GET: api/controller/5
        [HttpGet("{id}")]
        public virtual async Task<ActionResult<ApiResponse<TEntity>>> Get(int id)
        {
            if (!IsUserAuthorized("View"))
                return new ApiResponse<TEntity>().SetErrorResponse("error", "User is not authorized to perform this action.");


            TEntity? entity = await _dataService.GetGenericRepository<TEntity>().FindAsync(id);
            if (entity == null)
            {
                string className = typeof(TEntity).Name;
                return new ApiResponse<TEntity>().SetErrorResponse(className, $"Requested {className} not found!");
            }

            return new ApiResponse<TEntity>().SetSuccessResponse(entity);
        }

        // POST: api/controller
        [HttpPost]
        public virtual async Task<ActionResult<ApiResponse<TEntity>>> Post([FromBody] TEntityDto entityDto)
        {
            if (!IsUserAuthorized("Add"))
                return new ApiResponse<TEntity>().SetErrorResponse("Authorization", "User is not authorized to perform this action.");

            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<TEntity>().SetErrorResponse("Validation", "Invalid data provided."));


            TEntity entity = _mapper.Map<TEntity>(entityDto);

            try
            {
                int result = await _dataService.GetGenericRepository<TEntity>().AddAsync(entity);
                if (result != 1)
                    return new ApiResponse<TEntity>().SetErrorResponse("error", "An error occurred while creating the entity.");

                return new ApiResponse<TEntity>().SetSuccessResponse(entity);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error creating {EntityName}", typeof(TEntity).Name);
                return StatusCode(500, new ApiResponse<TEntity>().SetErrorResponse("Server", "An error occurred while creating the entity."));
            }
        }

        // PUT: api/controller/5
        [HttpPut("{id}")]
        public virtual async Task<ActionResult<ApiResponse<TEntity>>> Put(int id, [FromBody] TEntityDto entityDto)
        {
            if (!IsUserAuthorized("Edit"))
                return new ApiResponse<TEntity>().SetErrorResponse("Authorization", "User is not authorized to perform this action.");

            if (!ModelState.IsValid)
                return new ApiResponse<TEntity>().SetErrorResponse("Validation", "Invalid data provided.");

            TEntity entity = _mapper.Map<TEntity>(entityDto);


            TEntity? existingEntity = await _dataService.GetGenericRepository<TEntity>().FindAsync(id);
            if (existingEntity == null)
            {
                string className = typeof(TEntity).Name;
                return new ApiResponse<TEntity>().SetErrorResponse(className, $"Requested {className} not found!");
            }

            try
            {
                _dataService.Update(entity);
                return new ApiResponse<TEntity>().SetSuccessResponse(entity);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error updating {EntityName}", typeof(TEntity).Name);
                return StatusCode(500, new ApiResponse<TEntity>().SetErrorResponse("Server", "An error occurred while updating the entity."));
            }
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
                return new ApiResponse<TEntity>().SetErrorResponse(className, $"Requested {className} not found!");
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
            if (dataTable.MultiSortMeta?.Count() > 0)
            {
                // Create the first OrderBy().
                DataTableSortDto? dataTableSort = dataTable.MultiSortMeta.First();
                if (dataTableSort.Order > 0)
                    query.OrderBy(dataTableSort.Field.Substring(0,1).ToUpper() + dataTableSort.Field.Substring(1, dataTableSort.Field.Length), OrderDirectionEnum.ASCENDING);
                else if (dataTableSort.Order < 0)
                    query.OrderBy(dataTableSort.Field.Substring(0,1).ToUpper() + dataTableSort.Field.Substring(1, dataTableSort.Field.Length), OrderDirectionEnum.DESCENDING);

                // Create the rest OrderBy methods as ThenBy() if any.
                foreach (var sortInfo in dataTable.MultiSortMeta.Skip(1))
                {
                    if (dataTableSort.Order > 0)
                        query.ThenBy(sortInfo.Field.Substring(0, 1).ToUpper() + sortInfo.Field.Substring(1, sortInfo.Field.Length), OrderDirectionEnum.ASCENDING);
                    else if (dataTableSort.Order < 0)
                        query.ThenBy(sortInfo.Field.Substring(0, 1).ToUpper() + sortInfo.Field.Substring(1, sortInfo.Field.Length), OrderDirectionEnum.DESCENDING);
                }
            }


            // TODO add filters



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





        private bool IsUserAuthorized(string action)
        {
            string controllerName = ControllerContext.ActionDescriptor.ControllerName;
            string claimName = controllerName + "_" + action;

            return User.HasClaim(ClaimTypes.Role, claimName);
        }
    }
}