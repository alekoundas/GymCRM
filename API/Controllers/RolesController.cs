using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Linq.Expressions;
using Core.Dtos.Identity;
using Core.Dtos.DataTable;
using Business.Services;
using Core.Models;
using Core.System;
using Core.Dtos.Lookup;
using Core.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly ILogger<RolesController> _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TokenSettings _tokenSettings;

        public RolesController(
            IDataService dataService,
            ILogger<RolesController> logger,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            TokenSettings tokenSettings)
        {
            _dataService = dataService;
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenSettings = tokenSettings;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<IEnumerable<IdentityRole>> GetAll()
        {
            List<IdentityRole> result = await _roleManager.Roles.ToListAsync();
            return result;
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        public async Task<ApiResponse<IdentityRoleDto>> Get(string? id)
        {
            if (id == null)
                return new ApiResponse<IdentityRoleDto>().SetErrorResponse("errors", "Role ID not set!");

            IdentityRole? role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return new ApiResponse<IdentityRoleDto>().SetErrorResponse("errors", "Role not found!");

            IdentityRoleDto identityRoleDto = _mapper.Map<IdentityRoleDto>(role);
            return new ApiResponse<IdentityRoleDto>().SetSuccessResponse(identityRoleDto);
        }


        //[HttpGet("{roleName}/claims")]
        //public async Task<IActionResult> GetRoleClaims(string roleName)
        //{
        //    var role = await _roleManager.FindByNameAsync(roleName);
        //    if (role == null)
        //        return NotFound("Role not found");

        //    var claims = await _roleManager.GetClaimsAsync(role);
        //    return Ok(claims);
        //}

        // POST: api/controller/lookup
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {
            List<LookupOptionDto> lookupOptions;
            if (lookupDto.Filter?.Id != null && lookupDto.Filter?.Id.Length>0)
            {
                lookupOptions = await _roleManager.Roles
                    .Where(x=>lookupDto.Filter.Id.Contains(x.Id))
                    .Select(x => new LookupOptionDto() { Id = x.Id, Value = x.Name ?? "" })
                    .ToListAsync();
            }
            else if (lookupDto.Filter?.Value != null && lookupDto.Filter?.Value.Length > 0)
            {
                lookupOptions = await _roleManager.Roles
                    .Where(x => x.Name.Contains(lookupDto.Filter.Value))
                    .Select(x => new LookupOptionDto() { Id = x.Id, Value = x.Name ?? "" })
                    .ToListAsync();
            }
            else
            {

                lookupOptions = await _roleManager.Roles
                    .Select(x => new LookupOptionDto() { Id = x.Id, Value = x.Name ?? "" })
                    .ToListAsync();
            }

            lookupDto.Data = lookupOptions;

            return new ApiResponse<LookupDto>().SetSuccessResponse(lookupDto);
        }

        // POST: api/IdentityRoles/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<IdentityRoleDto>>> GetDataTable([FromBody] DataTableDto<IdentityRoleDto> dataTable)
        {
            //Func<IQueryable<IdentityRole>, IOrderedQueryable<IdentityRole>>? orderByQuery = null;
            //List<Func<IOrderedQueryable<IdentityRole>, IOrderedQueryable<IdentityRole>>>? thenOrderByQuery = new List<Func<IOrderedQueryable<IdentityRole>, IOrderedQueryable<IdentityRole>>>();
            //Expression<Func<IdentityRole, bool>>? filterQuery = new Expression<Func<IdentityRole, bool>>();


            //// Handle Sorting of DataTable.
            //if (dataTable.MultiSortMeta?.Count() > 0)
            //{
            //    // Create the first OrderBy().
            //    DataTableSortDto? dataTableSort = dataTable.MultiSortMeta.First();
            //    if (dataTableSort.Order > 0)
            //        orderByQuery = x => x.OrderByColumn(dataTableSort.Field);
            //    else if (dataTableSort.Order < 0)
            //        orderByQuery = x => x.OrderByColumnDescending(dataTableSort.Field);

            //    // Create the rest OrderBy methods as ThenBy() if any.
            //    foreach (var sortInfo in dataTable.MultiSortMeta.Skip(1))
            //    {
            //        if (dataTableSort.Order > 0)
            //            thenOrderByQuery.Add(x => x.ThenByColumn(sortInfo.Field));
            //        else if (dataTableSort.Order < 0)
            //            thenOrderByQuery.Add(x => x.ThenByColumnDescending(sortInfo.Field));
            //    }
            //}


            // Handle Filtering of DataTable.
            //if (dataTable.Filters?.FirstName?.Value != null && dataTable.Filters?.FirstName.Value.Length > 0)
            //    filterQuery.Add(x => x.FirstName.Contains(dataTable.Filters.FirstName.Value));

            //if (dataTable.Filters?.LastName?.Value != null && dataTable.Filters?.LastName.Value.Length > 0)
            //    filterQuery.Add(x => x.LastName.Contains(dataTable.Filters.LastName.Value));



            // Retrieve Data.
            List<IdentityRole> result = await _dataService.Query.Roles.ToListAsync();



            //    (
            //    filterQuery,
            //    orderByQuery,
            //    thenOrderByQuery,
            //    null,
            //    dataTable.PageCount.Value,
            //    dataTable.Page.Value
            //);
            List<IdentityRoleDto> IdentityRoleDto = _mapper.Map<List<IdentityRoleDto>>(result);

            //IdentityRoleDto.SelectMany(x => x.ContactInformations).ToList().ForEach(x => x.IdentityRole = null);
            //IdentityRoleDto.SelectMany(x => x.Tickets).ToList().ForEach(x => x.IdentityRole = null);

            //int rows = await _dataService.IdentityRoles.co(filterQuery);

            dataTable.Data = IdentityRoleDto;
            dataTable.PageCount = 0;

            return new ApiResponse<DataTableDto<IdentityRoleDto>>().SetSuccessResponse(dataTable);

        }

        // POST: api/Roles
        [HttpPost]
        public async Task<ApiResponse<IdentityRoleDto>> CreateRole([FromBody] IdentityRoleDto identityRoleDto)
        {


            if (string.IsNullOrWhiteSpace(identityRoleDto.Name))
                return new ApiResponse<IdentityRoleDto>().SetErrorResponse("errors", "Role name is required");


            var roleExists = await _roleManager.RoleExistsAsync(identityRoleDto.Name);
            if (roleExists)
                return new ApiResponse<IdentityRoleDto>().SetErrorResponse("errors", "Role already exists");

            IdentityRole identityRole = new IdentityRole(identityRoleDto.Name);
            var result = await _roleManager.CreateAsync(identityRole);
            if (result.Succeeded)
            {
                foreach (var item in identityRoleDto.Claims)
                {
                    if (item.View)
                        await _roleManager.AddClaimAsync(identityRole, new Claim(ClaimTypes.Role, item.Controller + "_View"));

                    if (item.Add)
                        await _roleManager.AddClaimAsync(identityRole, new Claim(ClaimTypes.Role, item.Controller + "_Add"));

                    if (item.Edit)
                        await _roleManager.AddClaimAsync(identityRole, new Claim(ClaimTypes.Role, item.Controller + "_Edit"));

                    if (item.Delete)
                        await _roleManager.AddClaimAsync(identityRole, new Claim(ClaimTypes.Role, item.Controller + "_Delete"));
                }
                return new ApiResponse<IdentityRoleDto>().SetSuccessResponse(identityRoleDto);
            }


            return new ApiResponse<IdentityRoleDto>().SetErrorResponse("errors", result.Errors.ToString() ?? "");
        }

        // PUT: api/Roles
        [HttpPut("{id}")]
        public async Task<ApiResponse<IdentityRoleDto>> Update(string? id, [FromBody] IdentityRoleDto identityRoleDto)
        {
            // Checks.
            if (id == null || id.Count() == 0)
                return new ApiResponse<IdentityRoleDto>().SetErrorResponse("error", "Role name not not set!");

            IdentityRole? identityRole = await _roleManager.FindByIdAsync(id);
            if (identityRole == null)
                return new ApiResponse<IdentityRoleDto>().SetErrorResponse("error", "Role name not found!");


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


            return new ApiResponse<IdentityRoleDto>().SetSuccessResponse(identityRoleDto);
        }


        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<ApiResponse<IdentityRoleDto>> DeleteRole(string? id)
        {
            if (id == null || id.Count() == 0)
                return new ApiResponse<IdentityRoleDto>().SetErrorResponse("error", "Role name not not set!");

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return new ApiResponse<IdentityRoleDto>().SetErrorResponse("error", "Role not found!");


            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
                return new ApiResponse<IdentityRoleDto>().SetSuccessResponse("success", $"Role {role.Name} deleted successfully");

            return new ApiResponse<IdentityRoleDto>().SetErrorResponse("error", result.Errors.ToString() ?? "");
        }
    }
}
