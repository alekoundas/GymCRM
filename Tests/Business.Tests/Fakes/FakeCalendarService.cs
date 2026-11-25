using Business.Services.CalendarService;
using Core.Models;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// Writes one file for each booking it is told to, and keeps what it was asked.
    /// </summary>
    internal sealed class FakeCalendarService : ICalendarService
    {
        public List<string> Lines { get; } = new List<string>();

        public int AddFiles { get; set; }

        public int EndFiles { get; set; }

        public int CancelFiles { get; set; }

        /// <summary>
        /// Hands back nothing at all, not even an empty list.
        /// </summary>
        public bool IsBroken { get; set; }

        public List<(string IcsContent, string FileName)> GenerateAddIcsContents(List<TrainGroupParticipant> trainGroupParticipants, string organizerEmail, string attendeeEmail, Guid userId)
        {
            Lines.Add($"add {trainGroupParticipants.Count} from {organizerEmail} for {attendeeEmail} {userId}");

            return IsBroken ? null! : Enumerable.Range(1, AddFiles).Select(x => ($"content of add {x}", $"add-{x}.ics")).ToList();
        }

        public List<(string IcsContent, string FileName)> GenerateEndIcsContents(List<TrainGroupParticipant> trainGroupParticipants, string organizerEmail, string attendeeEmail, Guid userId)
        {
            Lines.Add($"end {trainGroupParticipants.Count} from {organizerEmail} for {attendeeEmail} {userId}");

            return IsBroken ? null! : Enumerable.Range(1, EndFiles).Select(x => ($"content of end {x}", $"add-end-{x}.ics")).ToList();
        }

        public List<(string IcsContent, string FileName)> GenerateCancelIcsContents(List<TrainGroupParticipant> trainGroupParticipants, string organizerEmail, string attendeeEmail, Guid userId)
        {
            Lines.Add($"cancel {trainGroupParticipants.Count} from {organizerEmail} for {attendeeEmail} {userId}");

            return IsBroken ? null! : Enumerable.Range(1, CancelFiles).Select(x => ($"content of cancel {x}", $"cancel-{x}.ics")).ToList();
        }
    }
}
