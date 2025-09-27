using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Dtos.DataTable;
using Core.Dtos.Identity;
using Core.Models;
using Core.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ClaimsController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly ILogger<ClaimsController> _logger;
        private readonly IMapper _mapper;
        private readonly RoleManager<Role> _roleManager;

        public ClaimsController(
            IDataService dataService,
            ILogger<ClaimsController> logger,
            IMapper mapper,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<Role> roleManager,
            ClaimsIdentity claimsIdentity,
            TokenSettings tokenSettings)
        {
            _dataService = dataService;
            _logger = logger;
            _mapper = mapper;
            _roleManager = roleManager;
        }


        // POST: api/Claims/GetDataTable
        [HttpPost("GetDataTable")]
        public async Task<ApiResponse<DataTableDto<IdentityClaimDto>>> GetDataTable([FromBody] DataTableDto<IdentityClaimDto> dataTable)
        {

            List<IdentityClaimDto> identityClaimsDto = new List<IdentityClaimDto>();
            List<IdentityRoleClaim<Guid>> controllerNamess = _dataService.RoleClaims.ToList();
            List<string> controllerNames = _dataService.RoleClaims.ToList()
                .Select(x => x.ClaimValue.Split('_')[0])
                .Distinct()
                .ToList();


            string? roleName = dataTable.Filters.FirstOrDefault(x=>x.FieldName== "RoleName")?.Value;

            // View/Edit mode.
            if (roleName != null && roleName.Count() > 0)
            {
                Role? identityRole = await _roleManager.FindByNameAsync(roleName);
                if (identityRole != null)
                {
                    List<Claim> roleClaims = (await _roleManager.GetClaimsAsync(identityRole)).ToList();

                    foreach (var controller in controllerNames)
                        identityClaimsDto.Add(new IdentityClaimDto()
                        {
                            Controller = controller,
                            View = roleClaims.Any(x => x.Value == controller + "_View"),
                            Add = roleClaims.Any(x => x.Value == controller + "_Add"),
                            Edit = roleClaims.Any(x => x.Value == controller + "_Edit"),
                            Delete = roleClaims.Any(x => x.Value == controller + "_Delete"),
                        });
                }
            }
            // Add mode.
            else
                foreach (var controller in controllerNames)
                    identityClaimsDto.Add(new IdentityClaimDto()
                    {
                        Controller = controller,
                        View = false,
                        Add = false,
                        Edit = false,
                        Delete = false,
                    });


            //var claimstest = _claimsIdentity.Claims
            //    .Select(x => x.Value)
            //    .Select(x => new { controller = x.Split('_')[0], claim = x.Split('_')[1] })
            //    .ToList();



            dataTable.Data = identityClaimsDto;
            return new ApiResponse<DataTableDto<IdentityClaimDto>>().SetSuccessResponse(dataTable);
        }
    }
}
