using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Podium.Application.DTOs.SavedSearches;
using Podium.Application.Interfaces;
using Podium.Core.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Podium.API.Controllers
{
    [ApiController]
    [Route("api/band-staff/saved-searches")]
    [Authorize(Roles = "Recruiter")]
    public class SavedSearchController : ControllerBase
    {
        private readonly ISavedSearchService _savedSearchService;

        public SavedSearchController(ISavedSearchService savedSearchService)
        {
            _savedSearchService = savedSearchService;
        }

        private int GetBandStaffId()
        {
            var bandstaffIdClaim = User.FindFirst("bandStaffId")?.Value;
            return int.Parse(bandstaffIdClaim!);
        }

        /// <summary>
        /// Get all saved searches for the current recruiter
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SavedSearchSummaryDto>>> GetSavedSearches()
        {
            var bandStaffId = GetBandStaffId();
            var searches = await _savedSearchService.GetBandStaffSavedSearchesAsync(bandStaffId);
            return Ok(searches);
        }

        /// <summary>
        /// Get a specific saved search by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SavedSearchDto>> GetSavedSearch(int id)
        {
            try
            {
                var bandStaffId = GetBandStaffId();
                var search = await _savedSearchService.GetSavedSearchAsync(id, bandStaffId);
                return Ok(search);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Saved search not found" });
            }
        }

        /// <summary>
        /// Create a new saved search
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SavedSearchDto>> CreateSavedSearch([FromBody] CreateSavedSearchDto dto)
        {
            var bandStaffId = GetBandStaffId();
            var search = await _savedSearchService.CreateSavedSearchAsync(bandStaffId, dto);
            return CreatedAtAction(nameof(GetSavedSearch), new { id = search.Id }, search);
        }

        /// <summary>
        /// Update an existing saved search
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<SavedSearchDto>> UpdateSavedSearch(int id, [FromBody] UpdateSavedSearchDto dto)
        {
            try
            {
                var bandStaffId = GetBandStaffId();
                var search = await _savedSearchService.UpdateSavedSearchAsync(id, bandStaffId, dto);
                return Ok(search);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Saved search not found" });
            }
        }

        /// <summary>
        /// Delete a saved search
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSavedSearch(int id)
        {
            try
            {
                var bandStaffId = GetBandStaffId();
                await _savedSearchService.DeleteSavedSearchAsync(id, bandStaffId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Saved search not found" });
            }
        }

        /// <summary>
        /// Execute a search with filters (without saving)
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<StudentSearchResponse>> ExecuteSearch([FromBody] StudentSearchRequest request)
        {
            var bandStaffId = GetBandStaffId();
            var results = await _savedSearchService.ExecuteSearchAsync(bandStaffId, request);
            return Ok(results);
        }

        /// <summary>
        /// Execute a saved search by ID
        /// </summary>
        [HttpGet("{id}/execute")]
        public async Task<ActionResult<StudentSearchResponse>> ExecuteSavedSearch(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var bandStaffId = GetBandStaffId();
                var results = await _savedSearchService.ExecuteSavedSearchAsync(id, bandStaffId, page, pageSize);
                return Ok(results);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Saved search not found" });
            }
        }

        /// <summary>
        /// Get result count for search filters (preview without executing full search)
        /// </summary>
        [HttpPost("count")]
        public async Task<ActionResult<SearchResultCountDto>> GetSearchResultCount([FromBody] SearchFilterCriteria filters)
        {
            var bandStaffId = GetBandStaffId();
            var result = await _savedSearchService.GetSearchResultCountAsync(bandStaffId, filters);
            return Ok(result);
        }

        /// <summary>
        /// Share a saved search (generate shareable link)
        /// </summary>
        [HttpPost("{id}/share")]
        public async Task<ActionResult<ShareSearchDto>> ShareSearch(int id)
        {
            try
            {
                var bandStaffId = GetBandStaffId();
                var shareInfo = await _savedSearchService.ShareSearchAsync(id, bandStaffId);
                return Ok(shareInfo);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Saved search not found" });
            }
        }

        /// <summary>
        /// Unshare a saved search
        /// </summary>
        [HttpDelete("{id}/share")]
        public async Task<ActionResult> UnshareSearch(int id)
        {
            try
            {
                var bandStaffId = GetBandStaffId();
                await _savedSearchService.UnshareSearchAsync(id, bandStaffId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Saved search not found" });
            }
        }

        /// <summary>
        /// Get search templates (predefined common searches)
        /// </summary>
        [HttpGet("templates")]
        public async Task<ActionResult<List<SavedSearchSummaryDto>>> GetSearchTemplates()
        {
            var templates = await _savedSearchService.GetSearchTemplatesAsync();
            return Ok(templates);
        }

        /// <summary>
        /// Mark a search as used (updates usage statistics)
        /// </summary>
        [HttpPost("{id}/mark-used")]
        public async Task<ActionResult> MarkSearchUsed(int id)
        {
            try
            {
                var bandStaffId = GetBandStaffId();
                await _savedSearchService.MarkSearchUsedAsync(id, bandStaffId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Saved search not found" });
            }
        }

        /// <summary>
        /// Get new matches count for a saved search (for alert preview)
        /// </summary>
        [HttpGet("{id}/new-matches")]
        public async Task<ActionResult<int>> GetNewMatchesCount(int id)
        {
            var count = await _savedSearchService.GetNewMatchesCountAsync(id);
            return Ok(new { count });
        }
    }

    /// <summary>
    /// Public endpoint for shared searches (no authentication required)
    /// </summary>
    [ApiController]
    [Route("api/shared-searches")]
    public class SharedSearchController : ControllerBase
    {
        private readonly ISavedSearchService _savedSearchService;

        public SharedSearchController(ISavedSearchService savedSearchService)
        {
            _savedSearchService = savedSearchService;
        }

        /// <summary>
        /// Get results for a shared search using share token
        /// </summary>
        [HttpGet("{shareToken}")]
        [AllowAnonymous]
        public async Task<ActionResult<StudentSearchResponse>> GetSharedSearch(
            string shareToken,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var results = await _savedSearchService.GetSharedSearchResultsAsync(shareToken, page, pageSize);
                return Ok(results);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Shared search not found or no longer available" });
            }
        }
    }
}