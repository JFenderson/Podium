// Student DTOs matching Backend definitions strictly

export interface StudentDetailsDto {
  yearsOfExperience: any;
  studentId: number;
  firstName: string;
  lastName: string;
  email: string;
  
  // Bio & Description
  bio?: string;
  bioDescription?: string; // Backend getter alias for Bio
  
  // Contact & Location
  phoneNumber?: string;
  city?: string;
  state?: string;
  zipcode?: string;
  
  // Personal & Academic
  dateOfBirth?: Date;
  graduationYear?: number;
  highSchool?: string;
  gpa?: number;
  intendedMajor?: string;
  schoolType?: string;
  
  // Musical
  primaryInstrument?: string;
  secondaryInstruments: string[]; // Backend sends List<string>
  skillLevel?: string;
  achievements: string[];         // Backend sends List<string>
  awards?: string[];              // Backend getter alias for Achievements

  // Engagement / Metrics
  videoUrl?: string;
  videoThumbnailUrl?: string;
  averageRating?: number;
  ratingCount?: number;
  profileViews?: number;
  hasGuardian: boolean;
  
  // Lists
  // NOTE: Backend InterestDto only contains { studentId, bandId }. 
  // It does not currently return BandName or Date.
  interests: InterestDto[]; 
  
  // Timestamps
  createdAt: Date;
  updatedAt: Date;
}

export interface UpdateStudentDto {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  dateOfBirth?: Date;
  
  bioDescription?: string; // Backend maps this to Bio
  
  // Academic
  graduationYear?: number;
  highSchool?: string;
  gpa?: number;
  intendedMajor?: string;
  schoolType?: string;
  
  // Musical
  primaryInstrument?: string;
  skillLevel?: string;
  
  // Backend DTO defines this as 'string?', but usually frontend handles arrays.
  // If your backend requires a JSON string, you must JSON.stringify() this array before sending.
  secondaryInstruments?: string | string[]; 
  
  awards?: string[];
  achievements?: string[];

  // Location
  state?: string;
  city?: string;
  zipcode?: string;
}

// Matches Backend Podium.Application.DTOs.Student.StudentSummaryDto
export interface StudentSummaryDto {
  studentId: number;
  firstName: string;
  lastName: string;
  graduationYear?: number;
  primaryInstrument?: string;
  highSchool?: string;
  videoThumbnailUrl?: string;
  averageRating?: number;
  ratingCount?: number;
}

// Matches Backend Podium.Application.DTOs.Student.InterestDto
export interface InterestDto {
  studentId: number;
  bandId: number;
  bandName?: string;
  interestedAt: Date;
}

// Matches Backend Podium.Application.DTOs.Student.InterestedStudentDto
export interface InterestedStudentDto {
  studentId: number;
  name: string;
  email: string;
  phone?: string;
  primaryInstrument: string;
  skillLevel: string;
  graduationYear?: number;
  highSchool: string;
  state: string;
  interestedDate: Date;
  
  // Engagement
  videosUploaded: number;
  eventsAttended: number;
  hasBeenContacted: boolean;
  lastContactDate?: Date;
  hasOffer: boolean;
  offerStatus?: string;
  
  // Guardian Info
  hasGuardianLinked: boolean;
  requiresGuardianApproval: boolean;
}

export interface StudentFilterDto {
  search?: string;
  instrument?: string;
  graduationYear?: number;
  minGpa?: number;
  state?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface InterestedStudentFilterDto {
  instrument?: string;
  skillLevel?: string;
  graduationYear?: number;
  interestedAfter?: Date;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// src/app/core/models/student.models.ts

export interface StudentDashboardDto {
  studentId: number;
  firstName: string;
  lastName: string;
  primaryInstrument: string;
  profileImageUrl?: string;
  guardianInviteCode: string; 
  
  // Stats
  totalProfileViews: number;
  searchAppearances: number;
  activeOffers: number;
  pendingContactRequests: number;
  
  // Recent items
  recentNotifications: StudentNotificationDto[];
  recentActivity: StudentActivityDto[];
}

export interface StudentNotificationDto {
  id: number;
  title: string;
  message: string;
  type: string;
  createdAt: Date;
  isRead: boolean;
}

export interface StudentActivityDto {
  description: string;
  date: Date;
  icon?: string; // e.g. 'video', 'mail', 'award'
}