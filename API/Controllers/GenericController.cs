using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Business.Services;
using Core.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenericController<TEntity, TEntityController> : ControllerBase where TEntity : class
    {
        private readonly IDataService _dataService;
        private readonly ILogger<TEntityController> _logger;

        public GenericController(IDataService dataService, ILogger<TEntityController> logger)
        {
            _logger = logger;
            _dataService = dataService;
        }


        // GET: api/controller/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TEntity>>> Get(int id)
        {
            if (!IsUserAuthorized("Add"))
                return Unauthorized();


            TEntity? entity = await _dataService.GetGenericRepository<TEntity>().FindByIdAsync(id);
            if (entity == null)
            {
                string className = typeof(TEntity).Name;
                return new ApiResponse<TEntity>().SetErrorResponse(className, $"Requested {className} not found!");
            }

            return new ApiResponse<TEntity>().SetSuccessResponse(entity);
        }

        // DELETE: api/controller/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<TEntity>>> Delete(int id)
        {
            if (!IsUserAuthorized("Delete"))
                return Unauthorized();


            TEntity? entity = await _dataService.GetGenericRepository<TEntity>().FindByIdAsync(id);
            if (entity == null)
            {
                string className = typeof(TEntity).Name;
                return new ApiResponse<TEntity>().SetErrorResponse(className, $"Requested {className} not found!");
            }

             await _dataService.GetGenericRepository<TEntity>().SaveChangesAsync();
            return new ApiResponse<TEntity>().SetSuccessResponse(entity);
        }


        // POST: api/controller/lookup
        //[HttpPost("lookup")]
        //public async Task<ActionResult<ApiResponse<TEntity>>> Lookup([FromBody]LookupDto lookupDto)
        //{
        //    if (!IsUserAuthorized("View"))
        //        return Unauthorized();


        //        var aaa = _dataService.GetGenericRepository<TEntity>().GetLookup(null,null,lookupDto.Skip, lookupDto.Take);
        //    aaa.Select(x=>x.)



        //    TEntity? entity =


        //    if (entity == null)
        //    {
        //        string className = typeof(TEntity).Name;
        //        return new ApiResponse<TEntity>().SetErrorResponse(className, $"Requested {className} not found!");
        //    }

        //    return new ApiResponse<TEntity>().SetSuccessResponse(entity);
        //}


        private bool IsUserAuthorized(string action)
        {
            string controllerName = ControllerContext.ActionDescriptor.ControllerName;
            string claimName = controllerName + "_"+ action;

            return User.HasClaim(ClaimTypes.Role, claimName);
        }
    }
}