// saved-search.models.ts
// Saved Search DTOs matching backend SavedSearch feature

export interface SavedSearchDto {
  id: number;
  name: string;
  description?: string;
  filterCriteria: SearchFilterCriteria;
  alertsEnabled: boolean;
  alertFrequencyDays?: number;
  lastAlertSent?: Date;
  lastResultCount: number;
  isShared: boolean;
  shareToken?: string;
  isTemplate: boolean;
  createdAt: Date;
  updatedAt: Date;
  lastUsed?: Date;
  timesUsed: number;
}

export interface CreateSavedSearchDto {
  name: string;
  description?: string;
  filterCriteria: SearchFilterCriteria;
  alertsEnabled: boolean;
  alertFrequencyDays?: number;
  isTemplate: boolean;
}

export interface UpdateSavedSearchDto {
  name?: string;
  description?: string;
  filterCriteria?: SearchFilterCriteria;
  alertsEnabled?: boolean;
  alertFrequencyDays?: number;
}

export interface SavedSearchSummary {
  id: number;
  name: string;
  description?: string;
  currentResultCount: number;
  filterSummary: string;
  alertsEnabled: boolean;
  alertFrequencyDays?: number;
  lastUsed?: Date;
  updatedAt: Date;
}

export interface ShareSearchDto {
  shareToken?: string;
  shareUrl?: string;
}

// Search Filter Criteria - matches backend and your StudentSearchFilters
export interface SearchFilterCriteria {
  // Instruments
  instruments?: string[];
  
  // Location
  state?: string;
  hbcuOnly?: boolean;
  distanceRadius?: number;
  latitude?: number;
  longitude?: number;
  zipCode?: string;
  
  // Academics
  minGpa?: number;
  maxGpa?: number;
  graduationYear?: number;
  majors?: string[];
  
  // Experience
  skillLevels?: string[];
  minYearsExperience?: number;
  maxYearsExperience?: number;
  hasVideo?: boolean;
  hasAudio?: boolean;
  
  // Engagement
  showInterested?: boolean;
  showContacted?: boolean;
  showRated?: boolean;
  minRating?: number;
  maxRating?: number;
  
  // Sorting
  sortBy?: string;
  sortDirection?: string;
}

// Student Search Request (for executing searches)
export interface StudentSearchRequest {
  filters: SearchFilterCriteria;
  page: number;
  pageSize: number;
}

// Student Search Response
export interface StudentSearchResponse {
  results: StudentSearchResultDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  appliedFilters: SearchFilterCriteria;
}

// Student Search Result DTO - matches what backend returns
export interface StudentSearchResultDto {
  id: number;
  firstName: string;
  lastName: string;
  profileImageUrl?: string;
  primaryInstrument: string;
  allInstruments: string[];
  city?: string;
  state?: string;
  gpa?: number;
  graduationYear: number;
  intendedMajor?: string;
  skillLevel: string;
  yearsOfExperience: number;
  hasVideo: boolean;
  hasAudio: boolean;
  videoCount: number;
  audioCount: number;
  isInterested: boolean;
  hasBeenContacted: boolean;
  averageRating?: number;
  recruiterRating?: number;
  lastContactedAt?: Date;
}

// Search Result Count DTO
export interface SearchResultCountDto {
  count: number;
  filters: SearchFilterCriteria;
}