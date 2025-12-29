import { Injectable } from '@angular/core';
import { StudentSearchFilters } from '../../../core/models/student-search.models';
import { SearchFilterCriteria } from '../../../core/models/saved-search.models';

/**
 * Service to convert between StudentSearchFilters (your existing format)
 * and SearchFilterCriteria (new saved search backend format)
 */
@Injectable({
  providedIn: 'root'
})
export class FilterConverterService {

  /**
   * Convert StudentSearchFilters to SearchFilterCriteria (for saving to backend)
   */
  toSavedSearchFormat(filters: StudentSearchFilters): SearchFilterCriteria {
    return {
      // Instruments
      instruments: filters.instruments,
      
      // Location - SavedSearch uses single state, take first if multiple
      state: filters.states?.[0],
      hbcuOnly: filters.isHBCU,
      distanceRadius: filters.distance,
      zipCode: filters.zipCode,
      
      // Academics
      minGpa: filters.minGPA,
      maxGpa: filters.maxGPA,
      graduationYear: filters.graduationYears?.[0], // Take first year if multiple
      majors: filters.majors,
      
      // Experience
      skillLevels: filters.skillLevels,
      minYearsExperience: filters.minYearsExperience,
      maxYearsExperience: filters.maxYearsExperience,
      hasVideo: filters.hasVideo,
      hasAudio: filters.hasAuditionVideo, // Map audition video to hasAudio
      
      // Engagement
      showInterested: filters.isActivelyRecruiting,
      showContacted: undefined, // Add if you track this
      showRated: undefined, // Add if you track this
      
      // Sorting
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection
    };
  }

  /**
   * Convert SearchFilterCriteria to StudentSearchFilters (for loading from backend)
   */
  toStudentSearchFormat(criteria: SearchFilterCriteria): StudentSearchFilters {
    return {
      // Basic
      searchTerm: undefined,
      
      // Instruments
      instruments: criteria.instruments,
      
      // Location - Convert single state to array
      states: criteria.state ? [criteria.state] : undefined,
      isHBCU: criteria.hbcuOnly,
      distance: criteria.distanceRadius,
      zipCode: criteria.zipCode,
      
      // Academics
      minGPA: criteria.minGpa,
      maxGPA: criteria.maxGpa,
      graduationYears: criteria.graduationYear ? [criteria.graduationYear] : undefined,
      majors: criteria.majors,
      
      // Experience
      skillLevels: criteria.skillLevels,
      minYearsExperience: criteria.minYearsExperience,
      maxYearsExperience: criteria.maxYearsExperience,
      hasVideo: criteria.hasVideo,
      hasAuditionVideo: criteria.hasAudio,
      
      // Engagement
      isActivelyRecruiting: criteria.showInterested,
      isAvailable: undefined,
      hasScholarshipOffers: undefined,
      lastActivityDays: undefined,
      
      // Sorting
      sortBy: criteria.sortBy as any || 'relevance',
      sortDirection: criteria.sortDirection as any || 'desc',
      
      // Pagination
      page: 1,
      pageSize: 20
    };
  }

  /**
   * Convert saved search response to your StudentSearchResponse format
   */
  convertSearchResponse(savedSearchResponse: any): any {
    return {
      results: this.mapResultsToStudentFormat(savedSearchResponse.results),
      totalCount: savedSearchResponse.totalCount,
      page: savedSearchResponse.page,
      pageSize: savedSearchResponse.pageSize,
      totalPages: savedSearchResponse.totalPages,
      filters: this.toStudentSearchFormat(savedSearchResponse.appliedFilters),
      appliedFiltersCount: this.countActiveFilters(savedSearchResponse.appliedFilters)
    };
  }

  /**
   * Map backend search results to your StudentSearchResultDto format
   */
  private mapResultsToStudentFormat(results: any[]): any[] {
    return results.map(result => ({
      studentId: result.id,
      firstName: result.firstName,
      lastName: result.lastName,
      fullName: `${result.firstName} ${result.lastName}`,
      profilePhotoUrl: result.profileImageUrl,
      
      // Musical
      primaryInstrument: result.primaryInstrument,
      secondaryInstruments: result.allInstruments || [],
      skillLevel: result.skillLevel,
      yearsOfExperience: result.yearsOfExperience,
      
      // Location
      city: result.city || '',
      state: result.state || '',
      zipCode: result.zipCode,
      
      // Academic
      gpa: result.gpa,
      graduationYear: result.graduationYear,
      intendedMajor: result.intendedMajor,
      highSchool: result.highSchool,
      
      // Engagement
      videoCount: result.videoCount,
      hasAuditionVideo: result.hasVideo,
      profileViews: 0, // Not in backend response
      averageRating: result.averageRating,
      ratingCount: 0, // Not in backend response
      
      // Status
      accountStatus: 'Active',
      isAvailableForRecruiting: true,
      lastActivityDate: result.lastContactedAt || new Date(),
      
      // Recruiter-specific
      isWatchlisted: false, // Handle client-side
      hasSentOffer: false,
      hasContactRequest: result.hasBeenContacted,
      matchScore: undefined
    }));
  }

  /**
   * Count active filters in saved search format
   */
  countActiveFilters(criteria: SearchFilterCriteria): number {
    let count = 0;
    
    if (criteria.instruments?.length) count++;
    if (criteria.state) count++;
    if (criteria.hbcuOnly) count++;
    if (criteria.distanceRadius) count++;
    if (criteria.minGpa !== undefined || criteria.maxGpa !== undefined) count++;
    if (criteria.graduationYear) count++;
    if (criteria.majors?.length) count++;
    if (criteria.skillLevels?.length) count++;
    if (criteria.minYearsExperience !== undefined || criteria.maxYearsExperience !== undefined) count++;
    if (criteria.hasVideo) count++;
    if (criteria.hasAudio) count++;
    if (criteria.showInterested) count++;
    if (criteria.showContacted) count++;
    if (criteria.showRated) count++;
    
    return count;
  }
}