using AutoMapper;
using Core.Dtos.TrainGroupDate;
using Core.Models;

namespace API.AutoMapper
{
    public class FixedDayResolver : IValueResolver< TrainGroupDateDto, TrainGroupDate, DateOnly?>
    {
        public DateOnly? Resolve( TrainGroupDateDto source, TrainGroupDate destination, DateOnly? destMember, ResolutionContext context)
        {
            return source.FixedDay.HasValue ? DateOnly.FromDateTime(source.FixedDay.Value) : null;
        }

        public DateOnly? Resolve(TrainGroupDate source, TrainGroupDateDto destination, DateOnly? destMember, ResolutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
