using Coach.API.Data.Models;
using Coach.API.Data;
using Coach.API.Data.Repositories;
using Coach.API.Features.Coaches.GetCoaches;
using Microsoft.EntityFrameworkCore;
using Coach.API.Extensions;

namespace Coach.API.Features.Coaches.GetCoachById
{
    public record GetCoachByIdQuery(Guid Id) : IQuery<CoachResponse>;

    public class GetCoachByIdQueryValidator : AbstractValidator<GetCoachByIdQuery>
    {
        public GetCoachByIdQueryValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required");
        }
    }

    public class GetCoachByIdQueryHandler : IQueryHandler<GetCoachByIdQuery, CoachResponse>
    {
        private readonly ICoachRepository _coachRepository;
        private readonly ICoachSportRepository _sportRepository;
        private readonly ICoachPackageRepository _packageRepository;
        private readonly ICoachScheduleRepository _scheduleRepository; // Add this

        public GetCoachByIdQueryHandler(
            ICoachRepository coachRepository,
            ICoachSportRepository sportRepository,
            ICoachPackageRepository packageRepository,
            ICoachScheduleRepository scheduleRepository) // Add this
        {
            _coachRepository = coachRepository;
            _sportRepository = sportRepository;
            _packageRepository = packageRepository;
            _scheduleRepository = scheduleRepository; // Add this
        }

        public async Task<CoachResponse> Handle(GetCoachByIdQuery query, CancellationToken cancellationToken)
        {
            var coach = await _coachRepository.GetCoachByIdAsync(query.Id, cancellationToken);
            if (coach == null)
                throw new CoachNotFoundException(query.Id);

            var sports = await _sportRepository.GetCoachSportsByCoachIdAsync(coach.UserId, cancellationToken);
            var packages = await _packageRepository.GetCoachPackagesByCoachIdAsync(coach.UserId, cancellationToken);

            // Get coach schedules
            var schedules = await _scheduleRepository.GetCoachSchedulesByCoachIdAsync(coach.UserId, cancellationToken);

            var sportIds = sports.Select(s => s.SportId).ToList();
            var packageResponses = packages.Select(p => new CoachPackageResponse(
                p.Id, p.Name, p.Description, p.Price, p.SessionCount)).ToList();

            var weeklyScheduleResponses = schedules.ToWeeklyScheduleResponses();
            return new CoachResponse(
                coach.UserId,
                coach.FullName,
                coach.Email,
                coach.Phone,
                coach.Avatar,
                coach.GetImageUrlsList(),
                sportIds,
                coach.Bio,
                coach.RatePerHour,
                coach.CreatedAt,
                packageResponses,
                weeklyScheduleResponses); // Add this
        }
    }
}