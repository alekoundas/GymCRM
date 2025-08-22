using AutoMapper;
using Business.Services;
using Core.Dtos;
using Microsoft.AspNetCore.Mvc;
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






        private bool IsUserAuthorized(string action)
        {
            string controllerName = ControllerContext.ActionDescriptor.ControllerName;
            string claimName = controllerName + "_" + action;

            return User.HasClaim(ClaimTypes.Role, claimName);
        }
    }
}