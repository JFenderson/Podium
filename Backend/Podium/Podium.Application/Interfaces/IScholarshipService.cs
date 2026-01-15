using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.ScholarshipOffer;
using Podium.Application.Services;
using Podium.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.Interfaces
{
    public interface IScholarshipService
    {
        Task<ScholarshipOfferDto> CreateOfferAsync(CreateScholarshipOfferDto dto, string userId, bool isDirector);
        Task ApproveOfferAsync(int offerId, string directorId);
        Task RespondToOfferAsync(int offerId, RespondToScholarshipOfferDto dto, string userId, bool isGuardian);
        Task GuardianFinalizeOfferAsync(int offerId, string guardianUserId, bool accept);
        Task RescindOfferAsync(int offerId, RescindScholarshipRequest dto, string directorId);
        Task<ScholarshipBudgetDto> GetBudgetStatsAsync(int bandId);
        Task CheckExpirationsAsync(); // To be called by a background job
        Task<ScholarshipOverviewDto> GetScholarshipsAsync(string userId, ScholarshipFilterDto filters);
        Task<ScholarshipOfferDto> GetOfferByIdAsync(int id);
        Task<ServiceResult<PagedResult<OfferSummaryDto>>> GetStudentOfferSummariesAsync(int studentId, int page, int pageSize);
    }
}
