using AutoMapper;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.Chart;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ChartsController
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public ChartsController(IDataService dataService, IMapper mapper, IEmailService emailService)
        {
            _dataService = dataService;
            _mapper = mapper;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<ChartDataDto>>> GetChartData()
        {

            DateTime now = DateTime.UtcNow;
            DateTime start = now.AddDays(-6).Date;

            List<Mail> recentMails = await _dataService.Mails
                .Where(m => m.CreatedOn >= start)
                .ToListAsync();

            List<DailyEmailCountDto> dailyEmails = Enumerable.Range(0, 7).Select(i =>
            {
                var day = start.AddDays(i);
                var count = recentMails.Count(m => m.CreatedOn.Date == day.Date);
                return new DailyEmailCountDto { Date = day, Count = count };
            }).ToList();

            int usedLast24H = await _dataService.Mails.Where(m => m.CreatedOn >= now.AddHours(-24)).CountAsync();
            int availableEmails = 500 - usedLast24H;

            //ApiDbContext dbContext = _dataService.GetDbContext();
            var users = await _dataService.Users.OrderBy(x => x.CreatedOn).ToListAsync();

            var groupedUsers = users.GroupBy(u => new { u.CreatedOn.Year, u.CreatedOn.Month });
            List<UserGrowthDto> userGrowth = new List<UserGrowthDto>();
            int cumulative = 0;

            foreach (var group in groupedUsers)
            {
                cumulative += group.Count();
                DateTime date = new DateTime(group.Key.Year, group.Key.Month, 1);
                userGrowth.Add(new UserGrowthDto { Date = date, Cumulative = cumulative });
            }

            ChartDataDto chartData = new ChartDataDto
            {
                DailyEmails = dailyEmails,
                AvailableEmails = Math.Max(availableEmails, 0), // Prevent negative
                UserGrowth = userGrowth
            };

            return new ApiResponse<ChartDataDto>().SetSuccessResponse(chartData);
        }
    }
}
