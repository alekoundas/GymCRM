using Business.Services;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DataAccess
{
    public class ApiDbContextInitialiser
    {
        private readonly IDataService _dataService;
        private readonly ILogger<ApiDbContextInitialiser> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ClaimsIdentity _claimsIdentity;



        public ApiDbContextInitialiser(
            IDataService dataService,
            ILogger<ApiDbContextInitialiser> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ClaimsIdentity claimsIdentity)
        {
            _dataService = dataService;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _claimsIdentity = claimsIdentity;
        }

        public async Task RunMigrationsAsync()
        {
            try
            {
                if (_dataService.Query.Database.GetPendingMigrations().Any())
                    await _dataService.Query.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing migrations on the database.");
                throw;
            }
        }


        public async Task SeedAsync()
        {
            try
            {
                //TrySeedMakersAsync();
                TrySeedClaimsAsync();
                await TrySeedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
       

        private void TrySeedClaimsAsync()
        {
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Customers_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Customers_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Customers_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Customers_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Tickets_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Tickets_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Tickets_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Tickets_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "ContactInformations_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "ContactInformations_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "ContactInformations_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "ContactInformations_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Roles_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Roles_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Roles_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Roles_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Users_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Users_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Users_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Users_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Makers_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Makers_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Makers_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "Makers_Delete"));

            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "MakerModels_View"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "MakerModels_Add"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "MakerModels_Edit"));
            _claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "MakerModels_Delete"));

        }

        private async Task TrySeedAsync()
        {
            // Default roles
            var administratorRole = new IdentityRole("Administrator");

            if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
            {
                var role = await _roleManager.CreateAsync(administratorRole);
                if (role != null)
                    _claimsIdentity.Claims
                        .Select(x => x.Value)
                        .ToList()
                        .ForEach(async x =>
                            await _roleManager.AddClaimAsync(administratorRole, _claimsIdentity.Claims.First(y => y.Value == x))
                         );
            }

            // Default users
            var administrator = new ApplicationUser { UserName = "Admin", Email = "Admin@Admin.Admin" };

            if (_userManager.Users.All(u => u.UserName != administrator.UserName))
            {
                await _userManager.CreateAsync(administrator, "P@ssw0rd");
                if (!string.IsNullOrWhiteSpace(administratorRole.Name))
                {
                    await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
                }
            }
        }














        //private void TrySeedMakersAsync()
        //{
        //    List<Maker> makers = new List<Maker>();
        //    if (!_dataService.Makers.Any(null))
        //    {

        //        _dataService.Makers.Add(new Maker() { Title = "Acer Inc." });
        //        _dataService.Makers.Add(new Maker() { Title = "Admiral" });
        //        _dataService.Makers.Add(new Maker() { Title = "AGA AB" });
        //        _dataService.Makers.Add(new Maker() { Title = "Aiwa" });
        //        _dataService.Makers.Add(new Maker() { Title = "Akai" });
        //        _dataService.Makers.Add(new Maker() { Title = "Alba" });
        //        _dataService.Makers.Add(new Maker() { Title = "Amstrad" });
        //        _dataService.Makers.Add(new Maker() { Title = "Andrea Electronics" });
        //        _dataService.Makers.Add(new Maker() { Title = "Apex Digital" });
        //        _dataService.Makers.Add(new Maker() { Title = "Apple Inc." });
        //        _dataService.Makers.Add(new Maker() { Title = "Arcam" });
        //        _dataService.Makers.Add(new Maker() { Title = "Arise India" });
        //        _dataService.Makers.Add(new Maker() { Title = "Audiovox" });
        //        _dataService.Makers.Add(new Maker() { Title = "AWA" });
        //        _dataService.Makers.Add(new Maker() { Title = "Baird" });
        //        _dataService.Makers.Add(new Maker() { Title = "Bang & Olufsen" });
        //        _dataService.Makers.Add(new Maker() { Title = "Beko" });
        //        _dataService.Makers.Add(new Maker() { Title = "BenQ" });
        //        _dataService.Makers.Add(new Maker() { Title = "Binatone" });
        //        _dataService.Makers.Add(new Maker() { Title = "Blaupunkt" });
        //        _dataService.Makers.Add(new Maker() { Title = "BPL Group" });
        //        _dataService.Makers.Add(new Maker() { Title = "Brionvega" });
        //        _dataService.Makers.Add(new Maker() { Title = "Bush" });
        //        _dataService.Makers.Add(new Maker() { Title = "Canadian General Electric (CGE)" });
        //        _dataService.Makers.Add(new Maker() { Title = "Changhong" });
        //        _dataService.Makers.Add(new Maker() { Title = "ChiMei" });
        //        _dataService.Makers.Add(new Maker() { Title = "Compal Electronics" });
        //        _dataService.Makers.Add(new Maker() { Title = "Conar Instruments" });
        //        _dataService.Makers.Add(new Maker() { Title = "Continental Edison" });
        //        _dataService.Makers.Add(new Maker() { Title = "Cossor" });
        //        _dataService.Makers.Add(new Maker() { Title = "Craig" });
        //        _dataService.Makers.Add(new Maker() { Title = "Curtis Mathes Corporation" });
        //        _dataService.Makers.Add(new Maker() { Title = "Daewoo" });
        //        _dataService.Makers.Add(new Maker() { Title = "Dell" });
        //        _dataService.Makers.Add(new Maker() { Title = "Delmonico International Corporation" });
        //        _dataService.Makers.Add(new Maker() { Title = "DuMont Laboratories" });
        //        _dataService.Makers.Add(new Maker() { Title = "Durabrand" });
        //        _dataService.Makers.Add(new Maker() { Title = "Dynatron" });
        //        _dataService.Makers.Add(new Maker() { Title = "EKCO" });
        //        _dataService.Makers.Add(new Maker() { Title = "Electrohome" });
        //        _dataService.Makers.Add(new Maker() { Title = "Element Electronics" });
        //        _dataService.Makers.Add(new Maker() { Title = "Emerson Radio & Phonograph" });
        //        _dataService.Makers.Add(new Maker() { Title = "EMI" });
        //        _dataService.Makers.Add(new Maker() { Title = "English Electric" });
        //        _dataService.Makers.Add(new Maker() { Title = "English Electric Valve Company" });
        //        _dataService.Makers.Add(new Maker() { Title = "Farnsworth" });
        //        _dataService.Makers.Add(new Maker() { Title = "Ferguson Electronics" });
        //        _dataService.Makers.Add(new Maker() { Title = "Ferranti" });
        //        _dataService.Makers.Add(new Maker() { Title = "Finlux (Vestel)" });
        //        _dataService.Makers.Add(new Maker() { Title = "Fisher Electronics" });
        //        _dataService.Makers.Add(new Maker() { Title = "Fujitsu" });
        //        _dataService.Makers.Add(new Maker() { Title = "Funai" });
        //        _dataService.Makers.Add(new Maker() { Title = "Geloso" });
        //        _dataService.Makers.Add(new Maker() { Title = "General Electric" });
        //        _dataService.Makers.Add(new Maker() { Title = "General Electric Company" });
        //        _dataService.Makers.Add(new Maker() { Title = "GoldStar" });
        //        _dataService.Makers.Add(new Maker() { Title = "Goodmans Industries" });
        //        _dataService.Makers.Add(new Maker() { Title = "Google" });
        //        _dataService.Makers.Add(new Maker() { Title = "Gradiente" });
        //        _dataService.Makers.Add(new Maker() { Title = "Grundig" });
        //        _dataService.Makers.Add(new Maker() { Title = "Haier" });
        //        _dataService.Makers.Add(new Maker() { Title = "Hallicrafters" });
        //        _dataService.Makers.Add(new Maker() { Title = "Hannspree" });
        //        _dataService.Makers.Add(new Maker() { Title = "Heath Company/Heathkit" });
        //        _dataService.Makers.Add(new Maker() { Title = "Hinari Domestic Appliances" });
        //        _dataService.Makers.Add(new Maker() { Title = "Hisense" });
        //        _dataService.Makers.Add(new Maker() { Title = "Hitachi" });
        //        _dataService.Makers.Add(new Maker() { Title = "HMV" });
        //        _dataService.Makers.Add(new Maker() { Title = "Hoffman Television (Cortron Ind)" });
        //        _dataService.Makers.Add(new Maker() { Title = "Itel" });
        //        _dataService.Makers.Add(new Maker() { Title = "ITT Corporation" });
        //        _dataService.Makers.Add(new Maker() { Title = "Jensen Loudspeakers" });
        //        _dataService.Makers.Add(new Maker() { Title = "JVC" });
        //        _dataService.Makers.Add(new Maker() { Title = "Kenmore" });
        //        _dataService.Makers.Add(new Maker() { Title = "Kent Television" });
        //        _dataService.Makers.Add(new Maker() { Title = "Kloss Video" });
        //        _dataService.Makers.Add(new Maker() { Title = "Kogan" });
        //        _dataService.Makers.Add(new Maker() { Title = "Kolster-Brandes" });
        //        _dataService.Makers.Add(new Maker() { Title = "Konka" });
        //        _dataService.Makers.Add(new Maker() { Title = "Lanix" });
        //        _dataService.Makers.Add(new Maker() { Title = "Le.com" });
        //        _dataService.Makers.Add(new Maker() { Title = "LG Electronics" });
        //        _dataService.Makers.Add(new Maker() { Title = "Loewe" });
        //        _dataService.Makers.Add(new Maker() { Title = "Luxor" });
        //        _dataService.Makers.Add(new Maker() { Title = "Magnavox" });
        //        _dataService.Makers.Add(new Maker() { Title = "Marantz" });
        //        _dataService.Makers.Add(new Maker() { Title = "Marconiphone" });
        //        _dataService.Makers.Add(new Maker() { Title = "Matsui" });
        //        _dataService.Makers.Add(new Maker() { Title = "Memorex" });
        //        _dataService.Makers.Add(new Maker() { Title = "Metz" });
        //        _dataService.Makers.Add(new Maker() { Title = "Micromax" });
        //        _dataService.Makers.Add(new Maker() { Title = "Mitsubishi" });
        //        _dataService.Makers.Add(new Maker() { Title = "Mivar" });
        //        _dataService.Makers.Add(new Maker() { Title = "Motorola" });
        //        _dataService.Makers.Add(new Maker() { Title = "Muntz" });
        //        _dataService.Makers.Add(new Maker() { Title = "Murphy Radio" });
        //        _dataService.Makers.Add(new Maker() { Title = "NEC" });
        //        _dataService.Makers.Add(new Maker() { Title = "Nokia" });
        //        _dataService.Makers.Add(new Maker() { Title = "Nordmende" });
        //        _dataService.Makers.Add(new Maker() { Title = "Onida" });
        //        _dataService.Makers.Add(new Maker() { Title = "Orion" });
        //        _dataService.Makers.Add(new Maker() { Title = "Packard Bell" });
        //        _dataService.Makers.Add(new Maker() { Title = "Panasonic" });
        //        _dataService.Makers.Add(new Maker() { Title = "Pensonic" });
        //        _dataService.Makers.Add(new Maker() { Title = "Philco" });
        //        _dataService.Makers.Add(new Maker() { Title = "Philips" });
        //        _dataService.Makers.Add(new Maker() { Title = "Pioneer" });
        //        _dataService.Makers.Add(new Maker() { Title = "Planar Systems" });
        //        _dataService.Makers.Add(new Maker() { Title = "Polaroid" });
        //        _dataService.Makers.Add(new Maker() { Title = "ProLine" });
        //        _dataService.Makers.Add(new Maker() { Title = "ProScan" });
        //        _dataService.Makers.Add(new Maker() { Title = "Pye" });
        //        _dataService.Makers.Add(new Maker() { Title = "Pyle USA" });
        //        _dataService.Makers.Add(new Maker() { Title = "Quasar" });
        //        _dataService.Makers.Add(new Maker() { Title = "RadioShack" });
        //        _dataService.Makers.Add(new Maker() { Title = "Rauland-Borg" });
        //        _dataService.Makers.Add(new Maker() { Title = "RCA" });
        //        _dataService.Makers.Add(new Maker() { Title = "Realistic" });
        //        _dataService.Makers.Add(new Maker() { Title = "Rediffusion" });
        //        _dataService.Makers.Add(new Maker() { Title = "SABA" });
        //        _dataService.Makers.Add(new Maker() { Title = "Salora" });
        //        _dataService.Makers.Add(new Maker() { Title = "Salora International" });
        //        _dataService.Makers.Add(new Maker() { Title = "Samsung" });
        //        _dataService.Makers.Add(new Maker() { Title = "Sansui" });
        //        _dataService.Makers.Add(new Maker() { Title = "Sanyo" });
        //        _dataService.Makers.Add(new Maker() { Title = "Schneider Electric" });
        //        _dataService.Makers.Add(new Maker() { Title = "Seiki Digital" });
        //        _dataService.Makers.Add(new Maker() { Title = "Sèleco" });
        //        _dataService.Makers.Add(new Maker() { Title = "Setchell Carlson" });
        //        _dataService.Makers.Add(new Maker() { Title = "Sharp" });
        //        _dataService.Makers.Add(new Maker() { Title = "Siemens" });
        //        _dataService.Makers.Add(new Maker() { Title = "Skyworth" });
        //        _dataService.Makers.Add(new Maker() { Title = "Sony" });
        //        _dataService.Makers.Add(new Maker() { Title = "Soyo" });
        //        _dataService.Makers.Add(new Maker() { Title = "Stromberg-Carlson" });
        //        _dataService.Makers.Add(new Maker() { Title = "Supersonic" });
        //        _dataService.Makers.Add(new Maker() { Title = "Sylvania" });
        //        _dataService.Makers.Add(new Maker() { Title = "Symphonic" });
        //        _dataService.Makers.Add(new Maker() { Title = "Tandy" });
        //        _dataService.Makers.Add(new Maker() { Title = "Tatung Company" });
        //        _dataService.Makers.Add(new Maker() { Title = "TCL Corporation" });
        //        _dataService.Makers.Add(new Maker() { Title = "Technics" });
        //        _dataService.Makers.Add(new Maker() { Title = "TECO" });
        //        _dataService.Makers.Add(new Maker() { Title = "Teleavia" });
        //        _dataService.Makers.Add(new Maker() { Title = "Telefunken" });
        //        _dataService.Makers.Add(new Maker() { Title = "Teletronics" });
        //        _dataService.Makers.Add(new Maker() { Title = "Thomson SA" });
        //        _dataService.Makers.Add(new Maker() { Title = "Thorn Electrical Industries" });
        //        _dataService.Makers.Add(new Maker() { Title = "Thorn EMI" });
        //        _dataService.Makers.Add(new Maker() { Title = "Toshiba" });
        //        _dataService.Makers.Add(new Maker() { Title = "TP Vision" });
        //        _dataService.Makers.Add(new Maker() { Title = "TPV Technology" });
        //        _dataService.Makers.Add(new Maker() { Title = "Ultra" });
        //        _dataService.Makers.Add(new Maker() { Title = "Vestel" });
        //        _dataService.Makers.Add(new Maker() { Title = "Videocon" });
        //        _dataService.Makers.Add(new Maker() { Title = "Videoton" });
        //        _dataService.Makers.Add(new Maker() { Title = "Vizio" });
        //        _dataService.Makers.Add(new Maker() { Title = "Vu Televisions" });
        //        _dataService.Makers.Add(new Maker() { Title = "Walton" });
        //        _dataService.Makers.Add(new Maker() { Title = "Westinghouse Electronics" });
        //        _dataService.Makers.Add(new Maker() { Title = "White-Westinghouse" });
        //        _dataService.Makers.Add(new Maker() { Title = "Xiaomi" });
        //        _dataService.Makers.Add(new Maker() { Title = "Zanussi" });
        //        _dataService.Makers.Add(new Maker() { Title = "Zenith Radio" });
        //        _dataService.Makers.Add(new Maker() { Title = "Zonda" });

        //    }
    }
}