using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Identity;
using Core.Dtos.Lookup;
using Core.Enums;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly ILogger<RolesController> _logger;
        private readonly IMapper _mapper;
        private readonly RoleManager<Role> _roleManager;

        public RolesController(
            IDataService dataService,
            ILogger<RolesController> logger,
            IMapper mapper,
            RoleManager<Role> roleManager)
        {
            _dataService = dataService;
            _logger = logger;
            _mapper = mapper;
            _roleManager = roleManager;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<IEnumerable<Role>> GetAll()
        {
            List<Role> result = await _roleManager.Roles.ToListAsync();
            return result;
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        public async Task<ApiResponse<RoleDto>> Get(string? id)
        {
            if (id == null)
                return new ApiResponse<RoleDto>().SetErrorResponse("error", "Role ID not set!");

            Role? role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return new ApiResponse<RoleDto>().SetErrorResponse("error", "Role not found!");

            RoleDto identityRoleDto = _mapper.Map<RoleDto>(role);
            return new ApiResponse<RoleDto>().SetSuccessResponse(identityRoleDto);
        }

        // POST: api/Roles
        [HttpPost]
        public async Task<ApiResponse<List<RoleDto>>> CreateRole([FromBody] List<RoleDto> roleDto)
        {

            foreach (var role in roleDto)
            {
                if (string.IsNullOrWhiteSpace(role.Name))
                    return new ApiResponse<List<RoleDto>>().SetErrorResponse("error", "Role name is required");

                bool roleExists = await _dataService.Roles.AnyAsync(x => x.Name == role.Name);
                if (roleExists)
                    return new ApiResponse<List<RoleDto>>().SetErrorResponse("error", "Role already exists");

                Role identityRole = new Role();
                identityRole.Name = role.Name;

                var response = await _roleManager.CreateAsync(identityRole);
                if (response.Succeeded)
                    foreach (var claim in role.Claims)
                    {
                        if (claim.View)
                            await _roleManager.AddClaimAsync(identityRole, new Claim(ClaimTypes.Role, claim.Controller + "_View"));

                        if (claim.Add)
                            await _roleManager.AddClaimAsync(identityRole, new Claim(ClaimTypes.Role, claim.Controller + "_Add"));

                        if (claim.Edit)
                            await _roleManager.AddClaimAsync(identityRole, new Claim(ClaimTypes.Role, claim.Controller + "_Edit"));

                        if (claim.Delete)
                            await _roleManager.AddClaimAsync(identityRole, new Claim(ClaimTypes.Role, claim.Controller + "_Delete"));
                    }
                else
                    return new ApiResponse<List<RoleDto>>().SetErrorResponse("errors", response.Errors.ToString() ?? "");
            }

            return new ApiResponse<List<RoleDto>>().SetSuccessResponse(roleDto);
        }

        // PUT: api/Roles
        [HttpPut("{id}")]
        public async Task<ApiResponse<RoleDto>> Update(string? id, [FromBody] RoleDto identityRoleDto)
        {
            // Checks.
            if (id == null || id.Count() == 0)
                return new ApiResponse<RoleDto>().SetErrorResponse("error", "Role name not not set!");

            Role? identityRole = await _roleManager.FindByIdAsync(id);
            if (identityRole == null)
                return new ApiResponse<RoleDto>().SetErrorResponse("error", "Role name not found!");


            // Get claims from role.
            List<Claim> roleClaims = (await _roleManager.GetClaimsAsync(identityRole)).ToList();

            // Get claims from form.
            List<Claim> formClaims = new List<Claim>();
            identityRoleDto.Claims.ForEach(identityClaimDto =>
            {
                if (identityClaimDto.View)
                    formClaims.Add(new Claim(ClaimTypes.Role, identityClaimDto.Controller + "_View"));

                if (identityClaimDto.Add)
                    formClaims.Add(new Claim(ClaimTypes.Role, identityClaimDto.Controller + "_Add"));

                if (identityClaimDto.Edit)
                    formClaims.Add(new Claim(ClaimTypes.Role, identityClaimDto.Controller + "_Edit"));

                if (identityClaimDto.Delete)
                    formClaims.Add(new Claim(ClaimTypes.Role, identityClaimDto.Controller + "_Delete"));
            });

            // Get claims to delete from role, and remove them.
            List<Claim> deleteClaims = roleClaims.Where(x => !formClaims.Any(y => x.Value == y.Value)).ToList();
            deleteClaims.ForEach(async claim => await _roleManager.RemoveClaimAsync(identityRole, claim));

            // Get claims to add in role, and add them.
            List<Claim> addClaims = formClaims.Where(x => !roleClaims.Any(y => x.Value == y.Value)).ToList();
            addClaims.ForEach(async claim => await _roleManager.AddClaimAsync(identityRole, claim));


            return new ApiResponse<RoleDto>().SetSuccessResponse(identityRoleDto);
        }


        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<ApiResponse<RoleDto>> DeleteRole(string? id)
        {
            if (id == null || id.Count() == 0)
                return new ApiResponse<RoleDto>().SetErrorResponse("error", "Role name not not set!");

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return new ApiResponse<RoleDto>().SetErrorResponse("error", "Role not found!");


            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
                return new ApiResponse<RoleDto>().SetSuccessResponse("success", $"Role {role.Name} deleted successfully");

            return new ApiResponse<RoleDto>().SetErrorResponse("error", result.Errors.ToString() ?? "");
        }





        // POST: api/IdentityRoles/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<RoleDto>>> GetDataTable([FromBody] DataTableDto<RoleDto> dataTable)
        {
            var query = _dataService.Roles;

            // Handle Sorting of DataTable.
            if (dataTable.Sorts.Count() > 0)
            {
                // Create the first OrderBy().
                DataTableSortDto? dataTableSort = dataTable.Sorts.First();
                if (dataTableSort.Order > 0)
                    query.OrderBy(dataTableSort.FieldName, OrderDirectionEnum.ASCENDING);
                else if (dataTableSort.Order < 0)
                    query.OrderBy(dataTableSort.FieldName, OrderDirectionEnum.DESCENDING);

                // Create the rest OrderBy methods as ThenBy() if any.
                foreach (var sortInfo in dataTable.Sorts.Skip(1))
                {
                    if (dataTableSort.Order > 0)
                        query.ThenBy(sortInfo.FieldName, OrderDirectionEnum.ASCENDING);
                    else if (dataTableSort.Order < 0)
                        query.ThenBy(sortInfo.FieldName, OrderDirectionEnum.DESCENDING);
                }
            }


            // Handle pagination.
            int skip = (dataTable.Page - 1) * dataTable.Rows;
            int take = dataTable.Rows;

            query.AddPagging(skip, take);


            // Retrieve Data.
            List<Role> result = await query.ToListAsync();
            List<RoleDto> customerDto = _mapper.Map<List<RoleDto>>(result);

            //TODDO add filter
            int rows = await _dataService.Users.CountAsync();

            dataTable.Data = customerDto;
            dataTable.PageCount = rows;

            return new ApiResponse<DataTableDto<RoleDto>>().SetSuccessResponse(dataTable);

        }

        // POST: api/controller/lookup
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {
            var query = _dataService.GetGenericRepository<Role>();

            if (lookupDto.Filter?.Id != null && lookupDto.Filter?.Id.Length > 0)
                query = query.Where(x => lookupDto.Filter.Id.ToLower().Contains(x.Id.ToString().ToLower()));
            else if (lookupDto.Filter?.Value != null && lookupDto.Filter?.Value.Length > 0)
                query = query.Where(x => x.Name.ToLower().Contains(lookupDto.Filter.Value.ToLower()));


            // Handle Pagging.
            query.AddPagging(lookupDto.Skip, lookupDto.Take);

            // Retrieve Data.
            List<Role> result = await query.ToListAsync();
            lookupDto.Data = result
              .Select(x =>
                  new LookupOptionDto()
                  {
                      Id = x.Id.ToString(),
                      Value = x.Name,
                  })
              .ToList();

            return new ApiResponse<LookupDto>().SetSuccessResponse(lookupDto);
        }
    }
}
