// Rating DTOs matching backend

export interface RatingDto {
  ratingId: number;
  studentId: number;
  studentName?: string;
  bandStaffId: number;
  bandStaffName?: string;
  bandId: number;
  bandName?: string;
  overallRating: number;
  musicality?: number;
  technique?: number;
  marchingAbility?: number;
  leadership?: number;
  potential?: number;
  attitude?: number;
  comments?: string;
  isPublic: boolean;
  createdAt: Date;
  updatedAt?: Date;
}

export interface CreateRatingDto {
  studentId: number;
  overallRating: number;
  musicality?: number;
  technique?: number;
  marchingAbility?: number;
  leadership?: number;
  potential?: number;
  attitude?: number;
  comments?: string;
  isPublic?: boolean;
}

export interface UpdateRatingDto {
  overallRating?: number;
  musicality?: number;
  technique?: number;
  marchingAbility?: number;
  leadership?: number;
  potential?: number;
  attitude?: number;
  comments?: string;
  isPublic?: boolean;
}

export interface RatingCategoryDto {
  category: RatingCategory;
  value: number;
  maxValue: number;
}

export enum RatingCategory {
  Overall = 'Overall',
  Musicality = 'Musicality',
  Technique = 'Technique',
  MarchingAbility = 'MarchingAbility',
  Leadership = 'Leadership',
  Potential = 'Potential',
  Attitude = 'Attitude'
}

export interface StudentRatingSummaryDto {
  studentId: number;
  averageOverallRating: number;
  averageMusicality: number;
  averageTechnique: number;
  averageMarchingAbility: number;
  averageLeadership: number;
  averagePotential: number;
  averageAttitude: number;
  totalRatings: number;
  ratingDistribution: RatingDistributionDto[];
  recentRatings: RatingDto[];
}

export interface RatingDistributionDto {
  rating: number;
  count: number;
  percentage: number;
}

export interface BandStaffRatingStatsDto {
  bandStaffId: number;
  totalRatingsGiven: number;
  averageRatingGiven: number;
  studentsRated: number;
  mostRecentRating?: Date;
  ratingsByCategory: CategoryAverageDto[];
}

export interface CategoryAverageDto {
  category: RatingCategory;
  average: number;
}

export interface RatingFilterDto {
  studentId?: number;
  bandStaffId?: number;
  bandId?: number;
  minRating?: number;
  maxRating?: number;
  isPublic?: boolean;
  dateFrom?: Date;
  dateTo?: Date;
  pageNumber?: number;
  pageSize?: number;
}

export interface RatingSummaryDto {
  ratingId: number;
  studentName: string;
  bandStaffName: string;
  overallRating: number;
  createdAt: Date;
}

export interface RatingValidationDto {
  isValid: boolean;
  errors: string[];
}

// Validation rules
export const RatingRules = {
  minRating: 1,
  maxRating: 5,
  requiredFields: ['studentId', 'overallRating'],
  optionalCategories: ['musicality', 'technique', 'marchingAbility', 'leadership', 'potential', 'attitude'],
  maxCommentLength: 1000
} as const;