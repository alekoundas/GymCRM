using AutoMapper;
using Core.Dtos.TrainGroupDate;
using Core.Models;

namespace API.AutoMapper
{
    public class TrainGroupDateDtoDateTimeToDateOnlyResolver : IValueResolver<TrainGroupDateDto, TrainGroupDate, DateOnly?>
    {
        public DateOnly? Resolve(TrainGroupDateDto source, TrainGroupDate destination, DateOnly? destMember, ResolutionContext context)
        {
            if (source.FixedDay.HasValue)
                return DateOnly.FromDateTime(source.FixedDay.Value);

            return null;
        }
    }
}
