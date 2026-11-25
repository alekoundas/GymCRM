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

        // For the recurring bookings that were stopped and still have sessions before their
        // end: the series again, up to the last of them. Nothing for any other booking.
        List<(string IcsContent, string FileName)> GenerateEndIcsContents(
            List<TrainGroupParticipant> trainGroupParticipants,
            string organizerEmail,
            string attendeeEmail,
            Guid userId
        );

        // For the bookings nothing is left of. Those above get no file here.
        List<(string IcsContent, string FileName)> GenerateCancelIcsContents(
            List<TrainGroupParticipant> trainGroupParticipants,
            string organizerEmail,
            string attendeeEmail,
            Guid userId
        );
    }
}