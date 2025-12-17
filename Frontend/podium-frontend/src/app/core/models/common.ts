// Common models used across the application

export interface ServiceResult<T> {
  isSuccess: boolean;
  data?: T;
  errorMessage?: string;
  resultType: ServiceResultType;
}

export enum ServiceResultType {
  Success = 'Success',
  NotFound = 'NotFound',
  Forbidden = 'Forbidden',
  ValidationError = 'ValidationError',
  ServerError = 'ServerError'
}

export interface ApiError {
  message: string;
  errors?: { [key: string]: string[] };
  statusCode: number;
  timestamp: Date;
}

export interface NotificationDto {
  notificationId: number;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  relatedEntityId?: number;
  relatedEntityType?: string;
  isRead: boolean;
  createdAt: Date;
  readAt?: Date;
}

export enum NotificationType {
  OfferReceived = 'OfferReceived',
  OfferAccepted = 'OfferAccepted',
  OfferDeclined = 'OfferDeclined',
  OfferExpired = 'OfferExpired',
  ContactRequest = 'ContactRequest',
  ContactApproved = 'ContactApproved',
  ContactDenied = 'ContactDenied',
  RatingReceived = 'RatingReceived',
  GuardianApprovalNeeded = 'GuardianApprovalNeeded',
  VideoProcessed = 'VideoProcessed',
  VideoFailed = 'VideoFailed',
  System = 'System'
}

export interface RatingDto {
  ratingId?: number;
  studentId: number;
  bandStaffId: number;
  overallRating: number;
  musicality?: number;
  technique?: number;
  marchingAbility?: number;
  leadership?: number;
  comments?: string;
  createdAt?: Date;
}

export interface ContactRequestDto {
  requestId?: number;
  studentId: number;
  bandStaffId: number;
  bandId: number;
  message: string;
  status: ContactRequestStatus;
  studentResponse?: string;
  guardianApprovalStatus?: ApprovalStatus;
  createdAt?: Date;
  respondedAt?: Date;
}

export enum ContactRequestStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Denied = 'Denied',
  Expired = 'Expired'
}

export interface VideoUploadDto {
  studentId: number;
  title: string;
  description?: string;
  videoFile: File;
}

export interface VideoDto {
  videoId: number;
  studentId: number;
  title: string;
  description?: string;
  originalUrl: string;
  thumbnailUrl?: string;
  variants?: VideoVariantDto[];
  status: VideoStatus;
  uploadedAt: Date;
  processedAt?: Date;
}

export interface VideoVariantDto {
  quality: string; // '360p', '720p', '1080p'
  url: string;
  fileSize?: number;
}

export enum VideoStatus {
  Pending = 'Pending',
  Processing = 'Processing',
  Completed = 'Completed',
  Failed = 'Failed'
}

export interface BandStaffDto {
  bandStaffId: number;
  applicationUserId: string;
  firstName: string;
  lastName: string;
  role: string;
  email?: string;
  phoneNumber?: string;
  canViewStudents: boolean;
  canRateStudents: boolean;
  canSendOffers: boolean;
  canContactStudents: boolean;
  canManageEvents: boolean;
  canManageStaff: boolean;
  bandId: number;
  bandName?: string;
  createdAt: Date;
}

export interface GuardianDto {
  guardianId: number;
  applicationUserId: string;
  firstName: string;
  lastName: string;
  email?: string;
  phoneNumber?: string;
  relationship?: string;
  linkedStudents?: StudentSummaryDto[];
  createdAt: Date;
}

export interface StudentSummaryDto {
  studentId: number;
  firstName: string;
  lastName: string;
  email?: string;
  graduationYear?: number;
}

export interface DashboardStatsDto {
  totalStudents?: number;
  totalOffers?: number;
  pendingApprovals?: number;
  unreadNotifications?: number;
  recentActivity?: ActivityDto[];
}

export interface ActivityDto {
  id: string;
  type: string;
  description: string;
  timestamp: Date;
  relatedEntityId?: number;
}

export const Roles = {
  Student: 'Student',
  Guardian: 'Guardian',
  BandStaff: 'BandStaff',
  Director: 'Director',
  Admin: 'Admin'
} as const;

export type UserRole = typeof Roles[keyof typeof Roles];

export const Permissions = {
  ViewStudents: 'ViewStudents',
  RateStudents: 'RateStudents',
  ContactStudents: 'ContactStudents',
  SendOffers: 'SendOffers',
  ManageOffers: 'ManageOffers',
  ManageEvents: 'ManageEvents',
  ViewEvents: 'ViewEvents',
  ManageStaff: 'ManageStaff',
  ViewStaff: 'ViewStaff',
  ManageBand: 'ManageBand',
  ViewBandDetails: 'ViewBandDetails'
} as const;

export type Permission = typeof Permissions[keyof typeof Permissions];