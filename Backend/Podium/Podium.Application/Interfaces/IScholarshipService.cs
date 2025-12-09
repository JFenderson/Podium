using Podium.Application.DTOs.Offer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.Interfaces
{
    public interface IScholarshipService
    {
        Task<ScholarshipOfferDto> CreateOfferAsync(CreateOfferDto dto, string userId, bool isDirector);
        Task ApproveOfferAsync(int offerId, string directorId);
        Task RespondToOfferAsync(int offerId, RespondToOfferDto dto, string userId, bool isGuardian);
        Task RescindOfferAsync(int offerId, RescindScholarshipRequest dto, string directorId);
        Task<ScholarshipBudgetDto> GetBudgetStatsAsync(int bandId);
        Task CheckExpirationsAsync(); // To be called by a background job
        Task<ScholarshipOverviewDto> GetScholarshipsAsync(string userId, ScholarshipFilterDto filters);
    }
}
