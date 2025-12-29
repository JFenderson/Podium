using System;
using System.Collections.Generic;
using Podium.Core.Entities;

namespace Podium.Application.DTOs.SavedSearches
{
    public class SavedSearchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public SearchFilterCriteria FilterCriteria { get; set; } = new();
        public bool AlertsEnabled { get; set; }
        public int? AlertFrequencyDays { get; set; }
        public DateTime? LastAlertSent { get; set; }
        public int LastResultCount { get; set; }
        public bool IsShared { get; set; }
        public string? ShareToken { get; set; }
        public bool IsTemplate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastUsed { get; set; }
        public int TimesUsed { get; set; }
    }

    public class CreateSavedSearchDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public SearchFilterCriteria FilterCriteria { get; set; } = new();
        public bool AlertsEnabled { get; set; }
        public int? AlertFrequencyDays { get; set; }
        public bool IsTemplate { get; set; }
    }

    public class UpdateSavedSearchDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public SearchFilterCriteria? FilterCriteria { get; set; }
        public bool? AlertsEnabled { get; set; }
        public int? AlertFrequencyDays { get; set; }
    }

    public class SavedSearchSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CurrentResultCount { get; set; }
        public string FilterSummary { get; set; } = string.Empty;
        public bool AlertsEnabled { get; set; }
        public DateTime? LastUsed { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class StudentSearchResultDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string PrimaryInstrument { get; set; } = string.Empty;
        public List<string> AllInstruments { get; set; } = new();
        public string? City { get; set; }
        public string? State { get; set; }
        public double? Gpa { get; set; }
        public int? GraduationYear { get; set; }
        public string? IntendedMajor { get; set; }
        public string SkillLevel { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public bool HasVideo { get; set; }
        public bool HasAudio { get; set; }
        public int VideoCount { get; set; }
        public int AudioCount { get; set; }
        public bool IsInterested { get; set; }
        public bool HasBeenContacted { get; set; }
        public double? AverageRating { get; set; }
        public int? RecruiterRating { get; set; }
        public DateTime? LastContactedAt { get; set; }
        public double? DistanceInMiles { get; set; }
    }

    public class StudentSearchRequest
    {
        public SearchFilterCriteria Filters { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class StudentSearchResponse
    {
        public List<StudentSearchResultDto> Results { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public SearchFilterCriteria AppliedFilters { get; set; } = new();
    }

    public class SearchResultCountDto
    {
        public int Count { get; set; }
        public SearchFilterCriteria Filters { get; set; } = new();
    }

    public class ShareSearchDto
    {
        public string ShareUrl { get; set; } = string.Empty;
        public string ShareToken { get; set; } = string.Empty;
    }
}