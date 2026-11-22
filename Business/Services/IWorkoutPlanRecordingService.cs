using Core.Dtos.WorkoutPlanRecording;
using Core.Models;

namespace Business.Services
{
    public class WorkoutPlanRecordingResult
    {
        public bool IsSucceed { get; set; }
        public string ErrorKey { get; set; } = string.Empty;
        public string ErrorArgument { get; set; } = string.Empty;
        public WorkoutPlanRecording? Recording { get; set; }
    }

    public interface IWorkoutPlanRecordingService
    {
        // Everything the Start dialog needs, resolved server-side in one round trip.
        // Null when the plan does not exist, or is not the caller's and the caller is
        // not an admin.
        Task<WorkoutPlanStartContextDto?> GetStartContextAsync(int workoutPlanId, Guid callerId, bool callerIsAdmin);

        // The plan's member may start and stop their own workout. So may an admin
        // (callerIsAdmin), at the desk for a member; the recording still goes under the member.
        Task<WorkoutPlanRecordingResult> StartAsync(int workoutPlanId, int weekNumber, Guid callerId, bool callerIsAdmin);

        Task<WorkoutPlanRecordingResult> StopAsync(int recordingId, Guid callerId, bool callerIsAdmin);

        // True while the recording is open and still inside the cut-off.
        bool IsRunning(WorkoutPlanRecording recording, DateTime nowUtc);

        // True once an open recording has passed the cut-off - it can never be stopped.
        bool IsIncomplete(WorkoutPlanRecording recording, DateTime nowUtc);

        WorkoutPlanRecordingDto ToDto(WorkoutPlanRecording recording, DateTime nowUtc);
    }
}
