// Student DTOs matching backend

export interface StudentDetailsDto {
  studentId: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  dateOfBirth?: Date;
  graduationYear?: number;
  highSchool?: string;
  gpa?: number;
  instruments?: string[];
  primaryInstrument?: string;
  videoUrl?: string;
  videoThumbnailUrl?: string;
  bioDescription?: string;
  awards?: string[];
  interestedBands?: number[];
  averageRating?: number;
  ratingCount?: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface UpdateStudentDto {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  dateOfBirth?: Date;
  graduationYear?: number;
  highSchool?: string;
  gpa?: number;
  instruments?: string[];
  primaryInstrument?: string;
  bioDescription?: string;
  awards?: string[];
}

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

export interface InterestDto {
  studentId: number;
  bandId: number;
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

export interface StudentFilterDto {
  search?: string;
  instrument?: string;
  graduationYear?: number;
  minGpa?: number;
  state?: string;
  pageNumber?: number;
  pageSize?: number;
}