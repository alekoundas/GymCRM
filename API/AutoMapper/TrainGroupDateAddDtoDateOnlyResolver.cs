using AutoMapper;
using Core.Dtos.TrainGroupDate;
using Core.Models;

namespace API.AutoMapper
{
    public class TrainGroupDateAddDtoDateOnlyResolver : IValueResolver<TrainGroupDateAddDto, TrainGroupDate, DateOnly?>
    {
        public DateOnly? Resolve(TrainGroupDateAddDto source, TrainGroupDate destination, DateOnly? destMember, ResolutionContext context)
        {
            if (source.FixedDay.HasValue)
                return DateOnly.FromDateTime(source.FixedDay.Value);

            return null;
        }

        public DateOnly? Resolve(TrainGroupDate source, TrainGroupDateAddDto destination, DateOnly? destMember, ResolutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
