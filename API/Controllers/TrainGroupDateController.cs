
using AutoMapper;
using Business.Services;
using Core.Dtos.TrainGroupDate;
using Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    public class TrainGroupDateController : GenericController<TrainGroupDate, TrainGroupDateDto, TrainGroupDateAddDto>
    {
        private readonly IDataService _dataService;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public TrainGroupDateController(IDataService dataService, IMapper mapper) : base(dataService, mapper)
        {
            _dataService = dataService;
        }

        // Override Get to add custom logic
        //public override async Task<ActionResult<ApiResponse<TrainGroup>>> Get(int id)
        //{
        //    // Custom logic (e.g., include related data)
        //    var repository = _dataService.GetGenericRepository<TrainGroup>();
        //    var entity = await repository.FindAsync(id, include: source => source
        //        .Include(t => t.Trainer)
        //        .Include(t => t.RepeatingParticipants)
        //        .Include(t => t.TrainGroupDates));

        //    if (entity == null)
        //    {
        //        return NotFound(new ApiResponse<TrainGroup>().SetErrorResponse("TrainGroup", "Requested TrainGroup not found!"));
        //    }

        //    return Ok(new ApiResponse<TrainGroup>().SetSuccessResponse(entity));
        //}

        //// Add a custom endpoint
        //[HttpPost("{id}/add-participant")]
        //public async Task<ActionResult<ApiResponse<TrainGroup>>> AddParticipant(int id, [FromBody] Guid participantId)
        //{
        //    if (!IsUserAuthorized("AddParticipant"))
        //        return Unauthorized(new ApiResponse<TrainGroup>().SetErrorResponse("Authorization", "User is not authorized to perform this action."));

        //    // Custom logic to add a participant
        //    // Example: Add participant to RepeatingParticipants
        //    var trainGroup = await _dataService.GetGenericRepository<TrainGroup>().FindAsync(id);
        //    if (trainGroup == null)
        //        return NotFound(new ApiResponse<TrainGroup>().SetErrorResponse("TrainGroup", "Requested TrainGroup not found!"));

        //    var user = await _dataService.GetGenericRepository<User>().FindAsync(participantId);
        //    if (user == null)
        //        return NotFound(new ApiResponse<TrainGroup>().SetErrorResponse("User", "Requested User not found!"));

        //    trainGroup.RepeatingParticipants.Add(user);
        //    await _dataService.SaveChangesAsync();

        //    return Ok(new ApiResponse<TrainGroup>().SetSuccessResponse(trainGroup));
        //}
    }
}
