using Core.Models;

namespace Business.Services.CalendarService
{
    public interface ICalendarService
    {
        List<(string IcsContent, string FileName)> GenerateAddIcsContents(
            List<TrainGroupParticipant> trainGroupParticipants,
            string organizerEmail,
            string attendeeEmail,
            Guid userId
        );

        List<(string IcsContent, string FileName)> GenerateCancelIcsContents(
            List<TrainGroupParticipant> trainGroupParticipants,
            string organizerEmail,
            string attendeeEmail,
            Guid userId
        );
    }
}