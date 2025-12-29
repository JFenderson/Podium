using Podium.Application.DTOs.SavedSearches;
using Podium.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Podium.Application.Interfaces
{
    public interface ISavedSearchService
    {
        // Saved search management
        Task<SavedSearchDto> CreateSavedSearchAsync(int recruiterId, CreateSavedSearchDto dto);
        Task<SavedSearchDto> GetSavedSearchAsync(int id, int recruiterId);
        Task<List<SavedSearchSummaryDto>> GetBandStaffSavedSearchesAsync(int recruiterId);
        Task<SavedSearchDto> UpdateSavedSearchAsync(int id, int recruiterId, UpdateSavedSearchDto dto);
        Task DeleteSavedSearchAsync(int id, int recruiterId);
        Task<SavedSearchDto> MarkSearchUsedAsync(int id, int recruiterId);

        // Search execution
        Task<StudentSearchResponse> ExecuteSearchAsync(int recruiterId, StudentSearchRequest request);
        Task<StudentSearchResponse> ExecuteSavedSearchAsync(int savedSearchId, int recruiterId, int page = 1, int pageSize = 20);
        Task<SearchResultCountDto> GetSearchResultCountAsync(int recruiterId, SearchFilterCriteria filters);

        // Sharing
        Task<ShareSearchDto> ShareSearchAsync(int id, int recruiterId);
        Task<ShareSearchDto> UnshareSearchAsync(int id, int recruiterId);
        Task<StudentSearchResponse> GetSharedSearchResultsAsync(string shareToken, int page = 1, int pageSize = 20);

        // Templates
        Task<List<SavedSearchSummaryDto>> GetSearchTemplatesAsync();
        Task<SavedSearchDto> CreateTemplateAsync(CreateSavedSearchDto dto);

        // Alerts (called by background job)
        Task ProcessSearchAlertsAsync();
        Task<int> GetNewMatchesCountAsync(int savedSearchId);
    }
}