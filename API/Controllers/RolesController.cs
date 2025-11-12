using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.Identity;
using Core.Dtos.Lookup;
using Core.Models;
using Core.Translations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class RolesController : GenericController<Role, RoleDto, RoleDto>
    {
        private readonly IDataService _dataService;
        private readonly ILogger<RolesController> _logger;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer _localizer;
        private readonly RoleManager<Role> _roleManager;

        public RolesController(
            IDataService dataService,
            ILogger<RolesController> logger,
            IMapper mapper,
            IStringLocalizer localizer,
            RoleManager<Role> roleManager) : base(dataService, mapper, localizer)
        {
            _dataService = dataService;
            _logger = logger;
            _mapper = mapper;
            _localizer = localizer;
            _roleManager = roleManager;
        }



        // POST: api/Roles
        [HttpPost]
        public override async Task<ActionResult<ApiResponse<List<Role>>>> Post([FromBody] List<RoleDto> roleDto)
        {

            foreach (var role in roleDto)
            {
                if (string.IsNullOrWhiteSpace(role.Name))
                    return new ApiResponse<List<Role>>().SetErrorResponse(_localizer[TranslationKeys._0_is_required, "Role name"]);

                bool roleExists = await _dataService.Roles.AnyAsync(x => x.Name == role.Name);
                if (roleExists)
                    return new ApiResponse<List<Role>>().SetErrorResponse(_localizer[TranslationKeys._0_already_exists, "Role"]);

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
                    return new ApiResponse<List<Role>>().SetErrorResponse(response.Errors.ToString() ?? "");
            }

            List<Role> roles = _mapper.Map<List<Role>>(roleDto);
            return new ApiResponse<List<Role>>().SetSuccessResponse(roles, _localizer[TranslationKeys._0_updated_successfully, $"Role"]);
        }

        // PUT: api/Roles
        [HttpPut("{id}")]
        public override async Task<ActionResult<ApiResponse<Role>>> Put(string? id, [FromBody] RoleDto identityRoleDto)
        {
            // Checks.
            if (id == null)
                return new ApiResponse<Role>().SetErrorResponse(_localizer[TranslationKeys._0_is_required, "RoleId"]);

            Role? identityRole = await _roleManager.FindByIdAsync(id);
            if (identityRole == null)
                return new ApiResponse<Role>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, "Role name "]);


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


            Role updatedRole = _mapper.Map<Role>(identityRoleDto);
            return new ApiResponse<Role>().SetSuccessResponse(updatedRole, _localizer[TranslationKeys._0_updated_successfully, $"Role {updatedRole.Name}"]);
        }


        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public override async Task<ActionResult<ApiResponse<Role>>> Delete(string? id)
        {
            if (id == null)
                return new ApiResponse<Role>().SetErrorResponse(_localizer[TranslationKeys._0_is_required, "RoleId"]);

            var role = await _dataService.Roles.FindAsync(id);
            if (role == null)
                return new ApiResponse<Role>().SetErrorResponse(_localizer[TranslationKeys._0_not_found, "Role"]);


            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
                return new ApiResponse<Role>().SetSuccessResponse(_localizer[TranslationKeys._0_deleted_successfully, $"Role {role.Name}"]);

            return new ApiResponse<Role>().SetErrorResponse(result.Errors.ToString() ?? "");
        }


        // POST: api/controller/lookup
        [HttpPost("Lookup")]
        public async Task<ApiResponse<LookupDto>> Lookup([FromBody] LookupDto lookupDto)
        {
            var query = _dataService.GetGenericRepository<Role>();

            if (lookupDto.Filter?.Id != null && lookupDto.Filter?.Id.Length > 0)
                query = query.Where(x => lookupDto.Filter.Id.ToLower().Contains(x.Id.ToString().ToLower()));
            else if (lookupDto.Filter?.Value != null && lookupDto.Filter?.Value.Length > 0)
                query = query.Where(x => x.Name!.ToLower().Contains(lookupDto.Filter.Value.ToLower()));


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
