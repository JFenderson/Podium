using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.Band;
using Podium.Application.Interfaces;
using Podium.Core.Interfaces;

namespace Podium.Application.Services
{
    public class BandService : IBandService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BandService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<IEnumerable<BandSummaryDto>>> GetActiveBandsAsync(BandFilterDto filter)
        {
            // 1. Start Query (Global Filters for IsDeleted apply automatically)
            var query = _unitOfWork.Bands.GetQueryable()
                .Where(b => b.IsActive);

            // 2. Apply Filters
            if (!string.IsNullOrWhiteSpace(filter.State))
            {
                query = query.Where(b => b.State == filter.State);
            }

            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                query = query.Where(b => b.City == filter.City);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchTerm = filter.Search.ToLower();
                query = query.Where(b =>
                    b.BandName.ToLower().Contains(searchTerm) ||
                    (b.UniversityName != null && b.UniversityName.ToLower().Contains(searchTerm)));
            }

            // 3. Project to DTO
            var bands = await query
                .OrderBy(b => b.BandName)
                .Select(b => new BandSummaryDto
                {
                    Id = b.Id,
                    BandName = b.BandName,
                    UniversityName = b.UniversityName,
                    City = b.City,
                    State = b.State,
                    ShortDescription = b.Description != null && b.Description.Length > 100
                        ? b.Description.Substring(0, 100) + "..."
                        : b.Description
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<BandSummaryDto>>.Success(bands);
        }

        public async Task<ServiceResult<BandDetailDto>> GetBandDetailsAsync(int bandId)
        {
            // 1. Fetch Band with related data for counts
            var band = await _unitOfWork.Bands.GetQueryable()
                .Include(b => b.Events)
                .FirstOrDefaultAsync(b => b.Id == bandId && b.IsActive);

            if (band == null)
            {
                return ServiceResult<BandDetailDto>.Failure("Band not found");
            }

            // 2. Calculate dynamic properties
            var upcomingEvents = band.Events.Count(e => e.EventDate >= DateTime.UtcNow && !e.IsArchived);
            // Simple check: if budget > 0, they likely have scholarships
            var hasScholarships = band.ScholarshipBudget > 0;

            // 3. Map to DTO
            var dto = new BandDetailDto
            {
                Id = band.Id,
                BandName = band.BandName,
                UniversityName = band.UniversityName,
                City = band.City,
                State = band.State,
                Description = band.Description,
                Achievements = band.Achievements,
                UpcomingEventsCount = upcomingEvents,
                HasScholarshipsAvailable = hasScholarships
            };

            return ServiceResult<BandDetailDto>.Success(dto);
        }
    }
}