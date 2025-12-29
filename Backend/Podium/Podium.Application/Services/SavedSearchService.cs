using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.SavedSearches;
using Podium.Application.Interfaces;
using Podium.Core.Entities;
using Podium.Core.Interfaces;

namespace Podium.Application.Services
{
    public class SavedSearchService : ISavedSearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public SavedSearchService(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<SavedSearchDto> CreateSavedSearchAsync(int recruiterId, CreateSavedSearchDto dto)
        {
            var savedSearch = new SavedSearch
            {
                BandStaffId = recruiterId,
                Name = dto.Name,
                Description = dto.Description,
                FilterCriteria = JsonSerializer.Serialize(dto.FilterCriteria),
                AlertsEnabled = dto.AlertsEnabled,
                AlertFrequencyDays = dto.AlertFrequencyDays,
                IsTemplate = dto.IsTemplate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TimesUsed = 0,
                LastResultCount = 0
            };

            await _unitOfWork.SavedSearches.AddAsync(savedSearch);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(savedSearch);
        }

        public async Task<SavedSearchDto> GetSavedSearchAsync(int id, int recruiterId)
        {
            var savedSearch = await _unitOfWork.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == id && s.BandStaffId == recruiterId);

            if (savedSearch == null)
                throw new KeyNotFoundException("Saved search not found");

            return MapToDto(savedSearch);
        }

        public async Task<List<SavedSearchSummaryDto>> GetBandStaffSavedSearchesAsync(int recruiterId)
        {
            var savedSearches = await _unitOfWork.SavedSearches
                .FindAsync(s => s.BandStaffId == recruiterId && !s.IsTemplate);

            var orderedSearches = savedSearches
                .OrderByDescending(s => s.LastUsed ?? s.UpdatedAt)
                .ToList();

            var summaries = new List<SavedSearchSummaryDto>();

            foreach (var search in orderedSearches)
            {
                var filters = JsonSerializer.Deserialize<SearchFilterCriteria>(search.FilterCriteria);
                var count = await GetSearchResultCountInternalAsync(recruiterId, filters!);

                summaries.Add(new SavedSearchSummaryDto
                {
                    Id = search.Id,
                    Name = search.Name,
                    Description = search.Description,
                    CurrentResultCount = count,
                    FilterSummary = GenerateFilterSummary(filters!),
                    AlertsEnabled = search.AlertsEnabled,
                    LastUsed = search.LastUsed,
                    UpdatedAt = search.UpdatedAt
                });
            }

            return summaries;
        }

        public async Task<SavedSearchDto> UpdateSavedSearchAsync(int id, int recruiterId, UpdateSavedSearchDto dto)
        {
            var savedSearch = await _unitOfWork.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == id && s.BandStaffId == recruiterId);

            if (savedSearch == null)
                throw new KeyNotFoundException("Saved search not found");

            if (dto.Name != null) savedSearch.Name = dto.Name;
            if (dto.Description != null) savedSearch.Description = dto.Description;
            if (dto.FilterCriteria != null) savedSearch.FilterCriteria = JsonSerializer.Serialize(dto.FilterCriteria);
            if (dto.AlertsEnabled.HasValue) savedSearch.AlertsEnabled = dto.AlertsEnabled.Value;
            if (dto.AlertFrequencyDays.HasValue) savedSearch.AlertFrequencyDays = dto.AlertFrequencyDays.Value;

            savedSearch.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.SavedSearches.Update(savedSearch);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(savedSearch);
        }

        public async Task DeleteSavedSearchAsync(int id, int recruiterId)
        {
            var savedSearch = await _unitOfWork.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == id && s.BandStaffId == recruiterId);

            if (savedSearch == null)
                throw new KeyNotFoundException("Saved search not found");

            _unitOfWork.SavedSearches.Remove(savedSearch);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<SavedSearchDto> MarkSearchUsedAsync(int id, int recruiterId)
        {
            var savedSearch = await _unitOfWork.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == id && s.BandStaffId == recruiterId);

            if (savedSearch == null)
                throw new KeyNotFoundException("Saved search not found");

            savedSearch.LastUsed = DateTime.UtcNow;
            savedSearch.TimesUsed++;

            _unitOfWork.SavedSearches.Update(savedSearch);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(savedSearch);
        }

        public async Task<StudentSearchResponse> ExecuteSearchAsync(int recruiterId, StudentSearchRequest request)
        {
            var query = BuildSearchQuery(recruiterId, request.Filters);

            var totalCount = await query.CountAsync();

            var results = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new StudentSearchResponse
            {
                Results = results,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                AppliedFilters = request.Filters
            };
        }

        public async Task<StudentSearchResponse> ExecuteSavedSearchAsync(int savedSearchId, int recruiterId, int page = 1, int pageSize = 20)
        {
            var savedSearch = await _unitOfWork.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == savedSearchId && s.BandStaffId == recruiterId);

            if (savedSearch == null)
                throw new KeyNotFoundException("Saved search not found");

            var filters = JsonSerializer.Deserialize<SearchFilterCriteria>(savedSearch.FilterCriteria);

            // Mark as used
            savedSearch.LastUsed = DateTime.UtcNow;
            savedSearch.TimesUsed++;
            _unitOfWork.SavedSearches.Update(savedSearch);
            await _unitOfWork.SaveChangesAsync();

            return await ExecuteSearchAsync(recruiterId, new StudentSearchRequest
            {
                Filters = filters!,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<SearchResultCountDto> GetSearchResultCountAsync(int recruiterId, SearchFilterCriteria filters)
        {
            var count = await GetSearchResultCountInternalAsync(recruiterId, filters);

            return new SearchResultCountDto
            {
                Count = count,
                Filters = filters
            };
        }

        private async Task<int> GetSearchResultCountInternalAsync(int recruiterId, SearchFilterCriteria filters)
        {
            var query = BuildSearchQuery(recruiterId, filters);
            return await query.CountAsync();
        }

        private IQueryable<StudentSearchResultDto> BuildSearchQuery(int recruiterId, SearchFilterCriteria filters)
        {
            // Get BandStaff to find their BandId for interest filtering
            var bandStaffQuery = _unitOfWork.BandStaff.GetQueryable();
            var bandStaff = bandStaffQuery.FirstOrDefault(bs => bs.Id == recruiterId);
            var bandId = bandStaff?.BandId;

            // Base query with all student data
            var query = from student in _unitOfWork.Students.GetQueryable()
                        where student.IsActive && !student.IsDeleted
                        select new StudentSearchResultDto
                        {
                            Id = student.Id,
                            FirstName = student.FirstName,
                            LastName = student.LastName,
                            ProfileImageUrl = student.ProfilePhotoUrl,
                            PrimaryInstrument = student.PrimaryInstrument ?? "",
                            AllInstruments = student.SecondaryInstruments ?? new List<string>(),
                            City = student.City,
                            State = student.State,
                            Gpa = student.GPA.HasValue ? (double)student.GPA.Value : null,
                            GraduationYear = student.GraduationYear,
                            IntendedMajor = student.IntendedMajor,
                            SkillLevel = student.SkillLevel ?? "Beginner",
                            YearsOfExperience = student.YearsExperience ?? 0,
                            HasVideo = _unitOfWork.Videos.GetQueryable().Any(v => v.StudentId == student.Id && !v.IsDeleted),
                            HasAudio = false, // You don't have audio files
                            VideoCount = _unitOfWork.Videos.GetQueryable().Count(v => v.StudentId == student.Id && !v.IsDeleted),
                            AudioCount = 0,
                            IsInterested = bandId.HasValue && _unitOfWork.StudentInterests!.GetQueryable()
                                .Any(si => si.StudentId == student.Id && si.BandId == bandId.Value && si.IsInterested),
                            HasBeenContacted = _unitOfWork.ContactRequests.GetQueryable()
                                .Any(cr => cr.StudentId == student.Id && cr.BandStaffId == recruiterId),
                            AverageRating = _unitOfWork.VideoRatings.GetQueryable()
                                .Where(vr => vr.Video.StudentId == student.Id)
                                .Average(vr => (double?)vr.Rating),
                            RecruiterRating = _unitOfWork.VideoRatings.GetQueryable()
                                .Where(vr => vr.Video.StudentId == student.Id && vr.BandStaffId == recruiterId)
                                .Select(vr => (int?)vr.Rating)
                                .FirstOrDefault(),
                            LastContactedAt = _unitOfWork.ContactLogs.GetQueryable()
                                .Where(cl => cl.StudentId == student.Id && cl.BandStaffId == recruiterId)
                                .OrderByDescending(cl => cl.CreatedAt)
                                .Select(cl => (DateTime?)cl.CreatedAt)
                                .FirstOrDefault()
                        };

            // Apply filters
            if (filters.Instruments?.Any() == true)
            {
                query = query.Where(x => filters.Instruments.Contains(x.PrimaryInstrument));
            }

            if (!string.IsNullOrEmpty(filters.State))
            {
                query = query.Where(x => x.State == filters.State);
            }

            if (filters.HbcuOnly == true && bandId.HasValue)
            {
                // Filter for students interested in HBCU bands
                var hbcuBandIds = _unitOfWork.Bands.GetQueryable()
                    .Where(b => b.IsHbcu)
                    .Select(b => b.Id)
                    .ToList();

                query = query.Where(x => _unitOfWork.StudentInterests!.GetQueryable()
                    .Any(si => si.StudentId == x.Id && hbcuBandIds.Contains(si.BandId)));
            }

            if (filters.MinGpa.HasValue)
            {
                query = query.Where(x => x.Gpa >= filters.MinGpa.Value);
            }

            if (filters.MaxGpa.HasValue)
            {
                query = query.Where(x => x.Gpa <= filters.MaxGpa.Value);
            }

            if (filters.GraduationYear.HasValue)
            {
                query = query.Where(x => x.GraduationYear == filters.GraduationYear.Value);
            }

            if (filters.Majors?.Any() == true)
            {
                query = query.Where(x => filters.Majors.Contains(x.IntendedMajor!));
            }

            if (filters.SkillLevels?.Any() == true)
            {
                query = query.Where(x => filters.SkillLevels.Contains(x.SkillLevel));
            }

            if (filters.MinYearsExperience.HasValue)
            {
                query = query.Where(x => x.YearsOfExperience >= filters.MinYearsExperience.Value);
            }

            if (filters.MaxYearsExperience.HasValue)
            {
                query = query.Where(x => x.YearsOfExperience <= filters.MaxYearsExperience.Value);
            }

            if (filters.HasVideo == true)
            {
                query = query.Where(x => x.HasVideo);
            }

            if (filters.ShowInterested == true)
            {
                query = query.Where(x => x.IsInterested);
            }

            if (filters.ShowContacted == true)
            {
                query = query.Where(x => x.HasBeenContacted);
            }

            if (filters.ShowRated == true)
            {
                query = query.Where(x => x.RecruiterRating != null);
            }

            if (filters.MinRating.HasValue)
            {
                query = query.Where(x => x.AverageRating >= filters.MinRating.Value);
            }

            // Sorting
            query = ApplySorting(query, filters);

            return query;
        }

        private IQueryable<StudentSearchResultDto> ApplySorting(
            IQueryable<StudentSearchResultDto> query,
            SearchFilterCriteria filters)
        {
            var sortBy = filters.SortBy?.ToLower() ?? "recent";
            var isDescending = filters.SortDirection?.ToLower() != "asc";

            return sortBy switch
            {
                "gpa" => isDescending
                    ? query.OrderByDescending(x => x.Gpa)
                    : query.OrderBy(x => x.Gpa),
                "experience" => isDescending
                    ? query.OrderByDescending(x => x.YearsOfExperience)
                    : query.OrderBy(x => x.YearsOfExperience),
                "rating" => isDescending
                    ? query.OrderByDescending(x => x.AverageRating)
                    : query.OrderBy(x => x.AverageRating),
                _ => query.OrderByDescending(x => x.Id)
            };
        }

        public async Task<ShareSearchDto> ShareSearchAsync(int id, int recruiterId)
        {
            var savedSearch = await _unitOfWork.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == id && s.BandStaffId == recruiterId);

            if (savedSearch == null)
                throw new KeyNotFoundException("Saved search not found");

            if (string.IsNullOrEmpty(savedSearch.ShareToken))
            {
                savedSearch.ShareToken = GenerateShareToken();
                savedSearch.IsShared = true;
                savedSearch.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.SavedSearches.Update(savedSearch);
                await _unitOfWork.SaveChangesAsync();
            }

            return new ShareSearchDto
            {
                ShareToken = savedSearch.ShareToken,
                ShareUrl = $"/shared-search/{savedSearch.ShareToken}"
            };
        }

        public async Task<ShareSearchDto> UnshareSearchAsync(int id, int recruiterId)
        {
            var savedSearch = await _unitOfWork.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == id && s.BandStaffId == recruiterId);

            if (savedSearch == null)
                throw new KeyNotFoundException("Saved search not found");

            savedSearch.ShareToken = null;
            savedSearch.IsShared = false;
            savedSearch.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.SavedSearches.Update(savedSearch);
            await _unitOfWork.SaveChangesAsync();

            return new ShareSearchDto();
        }

        public async Task<StudentSearchResponse> GetSharedSearchResultsAsync(string shareToken, int page = 1, int pageSize = 20)
        {
            var savedSearches = await _unitOfWork.SavedSearches
                .FindAsync(s => s.ShareToken == shareToken && s.IsShared);

            var savedSearch = savedSearches.FirstOrDefault();

            if (savedSearch == null)
                throw new KeyNotFoundException("Shared search not found or no longer available");

            var filters = JsonSerializer.Deserialize<SearchFilterCriteria>(savedSearch.FilterCriteria);

            return await ExecuteSearchAsync(savedSearch.BandStaffId, new StudentSearchRequest
            {
                Filters = filters!,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<List<SavedSearchSummaryDto>> GetSearchTemplatesAsync()
        {
            var templates = await _unitOfWork.SavedSearches
                .FindAsync(s => s.IsTemplate);

            return templates.OrderBy(t => t.Name).Select(t => new SavedSearchSummaryDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                FilterSummary = GenerateFilterSummary(
                    JsonSerializer.Deserialize<SearchFilterCriteria>(t.FilterCriteria)!),
                AlertsEnabled = false,
                UpdatedAt = t.UpdatedAt
            }).ToList();
        }

        public async Task<SavedSearchDto> CreateTemplateAsync(CreateSavedSearchDto dto)
        {
            dto.IsTemplate = true;
            return await CreateSavedSearchAsync(0, dto);
        }

        public async Task ProcessSearchAlertsAsync()
        {
            var now = DateTime.UtcNow;

            var allSearches = await _unitOfWork.SavedSearches.GetAllAsync();

            var searchesNeedingAlerts = allSearches
                .Where(s => s.AlertsEnabled &&
                           (s.LastAlertSent == null ||
                            (s.AlertFrequencyDays.HasValue &&
                             s.LastAlertSent.Value.AddDays(s.AlertFrequencyDays.Value) <= now)))
                .ToList();

            foreach (var search in searchesNeedingAlerts)
            {
                try
                {
                    var newMatchesCount = await GetNewMatchesCountAsync(search.Id);

                    if (newMatchesCount > 0)
                    {
                        // Get BandStaff with ApplicationUser info
                        var bandStaffQuery = _unitOfWork.BandStaff.GetQueryable()
                            .Include(bs => bs.ApplicationUser);
                        var bandStaff = await bandStaffQuery.FirstOrDefaultAsync(bs => bs.Id == search.BandStaffId);

                        // Send email alert
                        var emailSent = await SendSearchAlertEmail(search, newMatchesCount, bandStaff);

                        // Create alert record
                        var alert = new SearchAlert
                        {
                            SavedSearchId = search.Id,
                            NewMatchesCount = newMatchesCount,
                            SentAt = now,
                            WasEmailSent = emailSent
                        };

                        await _unitOfWork.SearchAlerts.AddAsync(alert);

                        // Update saved search
                        search.LastAlertSent = now;
                        search.LastResultCount = newMatchesCount;
                        _unitOfWork.SavedSearches.Update(search);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other alerts
                    var alert = new SearchAlert
                    {
                        SavedSearchId = search.Id,
                        NewMatchesCount = 0,
                        SentAt = now,
                        WasEmailSent = false,
                        EmailError = ex.Message
                    };
                    await _unitOfWork.SearchAlerts.AddAsync(alert);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<int> GetNewMatchesCountAsync(int savedSearchId)
        {
            var savedSearch = await _unitOfWork.SavedSearches
                .FirstOrDefaultAsync(s => s.Id == savedSearchId);

            if (savedSearch == null) return 0;

            var filters = JsonSerializer.Deserialize<SearchFilterCriteria>(savedSearch.FilterCriteria);
            var currentCount = await GetSearchResultCountInternalAsync(savedSearch.BandStaffId, filters!);

            return Math.Max(0, currentCount - savedSearch.LastResultCount);
        }

        private async Task<bool> SendSearchAlertEmail(SavedSearch search, int newMatchesCount, BandStaff? bandStaff)
        {
            try
            {
                if (bandStaff?.ApplicationUser?.Email == null) return false;

                var subject = $"New matches for your saved search: {search.Name}";
                var body = $@"
                    <h2>You have {newMatchesCount} new matches!</h2>
                    <p>Your saved search ""{search.Name}"" has {newMatchesCount} new matching students.</p>
                    <p><a href='https://podium.app/recruiter/saved-searches/{search.Id}'>View matches</a></p>
                ";

                await _emailService.SendEmailAsync(bandStaff.ApplicationUser.Email, subject, body);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateFilterSummary(SearchFilterCriteria filters)
        {
            var parts = new List<string>();

            if (filters.Instruments?.Any() == true)
                parts.Add($"{filters.Instruments.Count} instrument(s)");

            if (!string.IsNullOrEmpty(filters.State))
                parts.Add(filters.State);

            if (filters.HbcuOnly == true)
                parts.Add("HBCU only");

            if (filters.MinGpa.HasValue || filters.MaxGpa.HasValue)
                parts.Add($"GPA {filters.MinGpa ?? 0:F1}-{filters.MaxGpa ?? 4.0:F1}");

            if (filters.GraduationYear.HasValue)
                parts.Add($"Class of {filters.GraduationYear}");

            if (filters.SkillLevels?.Any() == true)
                parts.Add(string.Join(", ", filters.SkillLevels));

            return parts.Any() ? string.Join(" • ", parts) : "No filters";
        }

        private string GenerateShareToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private SavedSearchDto MapToDto(SavedSearch entity)
        {
            return new SavedSearchDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                FilterCriteria = JsonSerializer.Deserialize<SearchFilterCriteria>(entity.FilterCriteria)!,
                AlertsEnabled = entity.AlertsEnabled,
                AlertFrequencyDays = entity.AlertFrequencyDays,
                LastAlertSent = entity.LastAlertSent,
                LastResultCount = entity.LastResultCount,
                IsShared = entity.IsShared,
                ShareToken = entity.ShareToken,
                IsTemplate = entity.IsTemplate,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                LastUsed = entity.LastUsed,
                TimesUsed = entity.TimesUsed
            };
        }
    }
}