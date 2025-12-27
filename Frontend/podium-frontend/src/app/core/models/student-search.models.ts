export interface StudentSearchFilters {
  // Search & Basic
  searchTerm?: string;
  
  newFilter?: string;
  // Instruments
  instruments?: string[];
  
  // Location
  states?: string[];
  isHBCU?: boolean;
  distance?: number;
  zipCode?: string;
  
  // Academics
  minGPA?: number;
  maxGPA?: number;
  graduationYears?: number[];
  majors?: string[];
  
  // Experience
  skillLevels?: string[];
  minYearsExperience?: number;
  maxYearsExperience?: number;
  hasVideo?: boolean;
  hasAuditionVideo?: boolean;
  
  // Engagement
  isAvailable?: boolean;
  isActivelyRecruiting?: boolean;
  hasScholarshipOffers?: boolean;
  lastActivityDays?: number;
  
  // Sorting
  sortBy?: 'relevance' | 'gpa' | 'experience' | 'recent' | 'rating' | 'name';
  sortDirection?: 'asc' | 'desc';
  
  // Pagination
  page?: number;
  pageSize?: number;
}

export interface StudentSearchResultDto {
  studentId: number;
  firstName: string;
  lastName: string;
  fullName: string;
  profilePhotoUrl?: string;
  
  // Primary Info
  primaryInstrument: string;
  secondaryInstruments?: string[];
  skillLevel: string;
  yearsOfExperience: number;
  
  // Location
  city: string;
  state: string;
  zipCode?: string;
  
  // Academic
  gpa?: number;
  graduationYear: number;
  intendedMajor?: string;
  highSchool?: string;
  
  // Engagement
  videoCount: number;
  hasAuditionVideo: boolean;
  profileViews: number;
  averageRating?: number;
  ratingCount: number;
  
  // Status
  accountStatus: string;
  isAvailableForRecruiting: boolean;
  lastActivityDate: Date;
  
  // Recruiter-specific
  isWatchlisted: boolean;
  hasSentOffer: boolean;
  hasContactRequest: boolean;
  matchScore?: number; // Relevance score for search
}

export interface StudentSearchResponse {
  results: StudentSearchResultDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  filters: StudentSearchFilters;
  appliedFiltersCount: number;
}

export interface SavedSearch {
  id: number;
  name: string;
  filters: StudentSearchFilters;
  createdDate: Date;
  lastUsedDate?: Date;
  resultCount?: number;
}

export interface QuickFilterChip {
  label: string;
  filterKey: string;
  value: any;
  removable: boolean;
}

// Autocomplete suggestions
export interface SearchSuggestion {
  text: string;
  type: 'student' | 'instrument' | 'location' | 'school';
  metadata?: any;
}

// Filter options for UI
export const SKILL_LEVELS = ['Beginner', 'Intermediate', 'Advanced', 'Expert'];

export const INSTRUMENTS = [
  'Flute', 'Piccolo', 'Clarinet',
  'Saxophone', 'Tenor Saxophone', 'Baritone',
  'Trumpet', 'Mellophone', 'Trombone', 'Baritone', 'Sousaphone',
  'Snare Drum', 'Tenor Drum', 'Bass Drum', 'Cymbals', 'Dance',
  'Color Guard'
];

export const US_STATES = [
  { code: 'AL', name: 'Alabama' },
  { code: 'AK', name: 'Alaska' },
  { code: 'AZ', name: 'Arizona' },
  { code: 'AR', name: 'Arkansas' },
  { code: 'CA', name: 'California' },
  { code: 'CO', name: 'Colorado' },
  { code: 'CT', name: 'Connecticut' },
  { code: 'DE', name: 'Delaware' },
  { code: 'FL', name: 'Florida' },
  { code: 'GA', name: 'Georgia' },
  { code: 'HI', name: 'Hawaii' },
  { code: 'ID', name: 'Idaho' },
  { code: 'IL', name: 'Illinois' },
  { code: 'IN', name: 'Indiana' },
  { code: 'IA', name: 'Iowa' },
  { code: 'KS', name: 'Kansas' },
  { code: 'KY', name: 'Kentucky' },
  { code: 'LA', name: 'Louisiana' },
  { code: 'ME', name: 'Maine' },
  { code: 'MD', name: 'Maryland' },
  { code: 'MA', name: 'Massachusetts' },
  { code: 'MI', name: 'Michigan' },
  { code: 'MN', name: 'Minnesota' },
  { code: 'MS', name: 'Mississippi' },
  { code: 'MO', name: 'Missouri' },
  { code: 'MT', name: 'Montana' },
  { code: 'NE', name: 'Nebraska' },
  { code: 'NV', name: 'Nevada' },
  { code: 'NH', name: 'New Hampshire' },
  { code: 'NJ', name: 'New Jersey' },
  { code: 'NM', name: 'New Mexico' },
  { code: 'NY', name: 'New York' },
  { code: 'NC', name: 'North Carolina' },
  { code: 'ND', name: 'North Dakota' },
  { code: 'OH', name: 'Ohio' },
  { code: 'OK', name: 'Oklahoma' },
  { code: 'OR', name: 'Oregon' },
  { code: 'PA', name: 'Pennsylvania' },
  { code: 'RI', name: 'Rhode Island' },
  { code: 'SC', name: 'South Carolina' },
  { code: 'SD', name: 'South Dakota' },
  { code: 'TN', name: 'Tennessee' },
  { code: 'TX', name: 'Texas' },
  { code: 'UT', name: 'Utah' },
  { code: 'VT', name: 'Vermont' },
  { code: 'VA', name: 'Virginia' },
  { code: 'WA', name: 'Washington' },
  { code: 'WV', name: 'West Virginia' },
  { code: 'WI', name: 'Wisconsin' },
  { code: 'WY', name: 'Wyoming' }
];

export const GRADUATION_YEARS = (() => {
  const currentYear = new Date().getFullYear();
  return Array.from({ length: 6 }, (_, i) => currentYear + i);
})();

export const COMMON_MAJORS = [
  'Music Education',
  'Music Performance',
  'Music Theory',
  'Music Composition',
  'Music Business',
  'Audio Engineering',
  'Undecided'
];