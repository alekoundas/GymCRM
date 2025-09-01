
using AutoMapper;
using Business.Services;
using Core.Dtos;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class TrainGroupParticipantsController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupParticipantsController(IDataService dataService, IMapper mapper)
        {
            _dataService = dataService;
            _mapper = mapper;
        }



        // PUT: api/controller/5
        [HttpPost("UpdateParticipants")]
        public async Task<ActionResult<ApiResponse<TrainGroup>>> UpdateParticipants([FromBody] TrainGroupParticipantUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", "Invalid data provided.");


            ApiDbContext dbContext = _dataService.GetDbContext();

            // Load existing entity with related data
            TrainGroup? existingEntity = await dbContext.Set<TrainGroup>()
                .Include(x => x.TrainGroupParticipants)
                .Include(x => x.TrainGroupDates)
                .ThenInclude(x => x.TrainGroupParticipants)
                .Where(x => x.Id == updateDto.TrainGroupId)
                .FirstOrDefaultAsync();

            if (existingEntity == null)
            {
                string className = typeof(TrainGroup).Name;
                return new ApiResponse<TrainGroup>().SetErrorResponse("error", $"Requested {className} not found!");
            }


            List<TrainGroupParticipant> incomingParticipants = _mapper.Map<List<TrainGroupParticipant>>(updateDto.TrainGroupParticipantDtos);
            List<TrainGroupParticipant> existingTrainGroupParticipants = existingEntity.TrainGroupParticipants
                .Where(x => x.UserId == new Guid(updateDto.UserId))
                .Where(x => x.SelectedDate == updateDto.SelectedDate)
                .ToList();
            List<TrainGroupParticipant> existingTrainGroupDateParticipants = existingEntity.TrainGroupDates
                .SelectMany(x => x.TrainGroupParticipants)
                .Where(x => x.UserId == new Guid(updateDto.UserId))
                .ToList();

            // Remove deleted TrainGroup
            foreach (TrainGroupParticipant existingParticipant in existingTrainGroupDateParticipants)
                if (!incomingParticipants.Any(d => d.Id == existingParticipant.Id && d.Id > 0))
                    dbContext.Remove(existingParticipant);

            // Remove deleted TrainGroupDates
            foreach (TrainGroupParticipant existingParticipant in existingTrainGroupParticipants)
                if (!incomingParticipants.Any(d => d.Id == existingParticipant.Id && d.Id > 0))
                    dbContext.Remove(existingParticipant);



            // Add TrainGroup Participants
            foreach (TrainGroupParticipant incomingParticipant in incomingParticipants)
            {
                bool existingTrainGroup = existingTrainGroupParticipants.Any(x => x.Id == incomingParticipant.Id);
                bool existingTrainGroupDate = existingTrainGroupDateParticipants.Any(x => x.Id == incomingParticipant.Id);
                if (!existingTrainGroup && !existingTrainGroupDate)
                {
                    // Add new TrainGroupDate
                    incomingParticipant.Id = 0;
                    dbContext.Add(incomingParticipant);
                    //existingEntity.TrainGroupParticipants.Add(incomingParticipant);
                }
            }


            await dbContext.SaveChangesAsync();
            dbContext.Dispose();
            return new ApiResponse<TrainGroup>().SetSuccessResponse(existingEntity);
        }

    }
}
