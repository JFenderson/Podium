// src/app/core/models/guardian.models.ts

export interface GuardianDto {
  guardianId: number;
  applicationUserId: string;
  firstName: string;
  lastName: string;
  email?: string;
  phoneNumber?: string;
  relationship?: string;
  linkedStudents?: GuardianLinkedStudentDto[];
  createdAt: Date;
  updatedAt?: Date;
}

export interface GuardianLinkedStudentDto {
  studentId: number;
  studentName: string;
  firstName: string;
  lastName: string;
  email?: string;
  graduationYear?: number;
  highSchool?: string;
  primaryInstrument?: string;
  
  // Stats & UI Flags
  pendingOffers: number;
  pendingApprovals: number;
  pendingContactRequests: number;
  activeScholarshipOffers: number;
  bandsInterested: number;
  lastActivityDate: Date;
  hasExpiringOffers: boolean;
  hasUrgentApprovals: boolean;

  accountStatus: string;           // e.g., 'Active', 'Pending', 'Suspended'
  requiresGuardianApproval: boolean; 
  isMinor: boolean;
}

export interface LinkStudentDto {
  studentEmail: string;
  relationship: string;
  verificationCode: string;
}

// FIX: Added missing DTOs that were causing build errors
export interface NotificationListDto {
  items: any[]; // You can replace 'any' with NotificationDto if you have it
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface GuardianNotificationPreferencesDto {
  emailNotifications: boolean;
  smsNotifications: boolean;
  inAppNotifications: boolean;
  alertTypes: string[];
}

export interface GuardianDashboardDto {
  // Data Lists
linkedStudents?: GuardianLinkedStudentDto[];
  pendingApprovals?: GuardianPendingApprovalDto[];
  recentActivity?: GuardianActivityDto[]; 
  recentActivities?: GuardianRecentActivityDto[];
  priorityAlerts?: PriorityAlertDto[];
  
  pendingContactRequests?: GuardianContactRequestDto[];
  scholarshipOffers?: GuardianScholarshipDto[];

  // Summary Stats
  totalOffers: number;
  pendingOffersCount: number;
  totalPendingApprovals: number;
  totalActiveOffers: number;
  totalUnreadNotifications: number;
}

export interface GuardianContactRequestDto {
  requestId: number;
  studentId: number;
  studentName: string;
 recruiterName?: string;
  recruiterRole: string;
  recruiterAvatarUrl?: string;
  bandName: string;
  message: string;
  sentAt: Date;
  expiresAt: Date;
}

export interface GuardianPendingApprovalDto {
  offerId: number;
  studentId: number;
  studentName: string;
  bandId: number;
  bandName: string;
  amount?: number;
  offerType?: string;
  description: string;
  sentAt: Date;
  expiresAt?: Date;
  dateReceived: Date;
}

export interface GuardianActivityDto {
  activityId: string;
  type: string;
  studentName: string;
  description: string;
  timestamp: Date;
}

export interface GuardianRecentActivityDto {
  activityType: string;
  description: string;
  timestamp: Date;
  studentName: string;
  iconType: string;
}

export interface PriorityAlertDto {
  alertType: string;
  message: string;
  studentId: number;
  studentName: string;
  deadline: Date;
  actionUrl: string;
  severity: 'High' | 'Medium' | 'Low';
}

export interface GuardianScholarshipDto {
  offerId: number;
  studentId: number;
  studentName: string;
  bandId: number;
  bandName: string;
  amount: number; // standardized
  scholarshipAmount: number; // alias if backend sends this
  offerType: string;
  status: string;
  createdAt: Date;
  expirationDate: Date;
  terms?: string;
  requiresGuardianApproval: boolean;
}

export interface StudentGuardianDto {
  guardianId: number;
  firstName: string;
  lastName: string;
  email?: string;
  relationship?: string;
  isPrimary: boolean;
}

export interface StudentProfileViewDto {
  studentId: number;
  name: string;
  email: string;
  primaryInstrument: string;
  graduationYear: number;
  highSchool: string;
  avatarUrl?: string;
  videosUploaded: number;
  bandsInterested: number;
  eventsAttended: number;

  accountStatus: string;
  requiresGuardianApproval: boolean;
  isMinor: boolean;
}

export interface StudentActivityReportDto {
  studentId: number;
  studentName: string;
  videosUploaded: any[];
  interestShown: InterestActivityDto[];
  offersReceived: OfferActivityDto[];
  eventsAttended: EventActivityDto[];
  contactsMade: any[];
}

export interface InterestActivityDto {
  bandName: string;
  university: string;
  interestDate: Date;
}

export interface OfferActivityDto {
  bandName: string;
  amount: number;
  offerDate: Date;
  status: string;
}

export interface EventActivityDto {
  eventName: string;
  bandName: string;
  eventDate: Date;
  didAttend: boolean;
}