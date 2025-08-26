using AutoMapper;
using Core.Dtos.TrainGroupDate;
using Core.Models;

namespace API.AutoMapper
{
    public class TrainGroupDateDtoDateOnlyToDateTimeResolver : IValueResolver<TrainGroupDate, TrainGroupDateDto, DateTime?>
    {
        public DateTime? Resolve(TrainGroupDate source, TrainGroupDateDto destination, DateTime? destMember, ResolutionContext context)
        {
            if (source.FixedDay.HasValue)
                return new DateTime(source.FixedDay.Value.Year, source.FixedDay.Value.Month, source.FixedDay.Value.Day, 0, 0, 0, 0);

            return null;
        }
    }
}
