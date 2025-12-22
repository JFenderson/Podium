// Guardian DTOs matching backend

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
  pendingOffers: number;
  pendingApprovals: number;

  pendingContactRequests: number;
  activeScholarshipOffers: number;
  bandsInterested: number;
  lastActivityDate: Date;
  hasExpiringOffers: boolean;
  hasUrgentApprovals: boolean;
}

export interface LinkStudentDto {
  studentEmail: string;
  relationship: string;
  verificationCode: string; // Optional, if you implement invite codes later
}

export interface GuardianDashboardDto {
  linkedStudents: GuardianLinkedStudentDto[];
  pendingApprovals: GuardianPendingApprovalDto[];
  recentActivity: GuardianActivityDto[];
  totalOffers: number;
  pendingOffersCount: number;
  priorityAlerts: PriorityAlertDto[];
  recentActivities: GuardianRecentActivityDto[];
}

export interface GuardianPendingApprovalDto {
  offerId: number;
  studentId: number;
  studentName: string;
  bandId: number;
  bandName: string;
  offerType?: string;
  amount?: number;
  description: string;
  expiresAt?: Date;
  sentAt: Date;
  offerDetails: string;
  dateReceived: Date;
  requestedAt: Date;
}

export interface GuardianActivityDto {
  activityId: string;
  type: GuardianActivityType;
  studentName: string;
  description: string;
  timestamp: Date;
  relatedEntityId?: number;
}

export enum GuardianActivityType {
  OfferReceived = 'OfferReceived',
  OfferAccepted = 'OfferAccepted',
  OfferDeclined = 'OfferDeclined',
  ApprovalRequested = 'ApprovalRequested',
  ApprovalGranted = 'ApprovalGranted',
  ApprovalDenied = 'ApprovalDenied',
  ContactRequested = 'ContactRequested',
  VideoUploaded = 'VideoUploaded',
  InterestShown = 'InterestShown',
}

export interface GuardianApprovalDto {
  offerId: number;
  approved: boolean;
  notes?: string;
}

export interface GuardianApprovalRequestDto {
  requestId: number;
  offerId: number;
  studentId: number;
  studentName: string;
  bandId: number;
  bandName: string;
  offerDetails: string;
  amount?: number;
  requestedAt: Date;
  status: ApprovalStatus;
}

export enum ApprovalStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Declined = 'Declined',
}

export interface StudentGuardianDto {
  guardianId: number;
  firstName: string;
  lastName: string;
  email?: string;
  phoneNumber?: string;
  relationship?: string;
  isPrimary: boolean;
}

export interface GuardianScholarshipDto {
  offerId: number;
  bandId: number;
  bandName: string;
  scholarshipAmount: number;
  status: string; // 'Sent', 'Accepted', 'Declined', 'expired'
  offerType: string;
  createdAt: Date;
  expirationDate: Date;
  terms?: string;
  requiresGuardianApproval: boolean;
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
}

export interface StudentActivityReportDto {
  studentId: number;
  studentName: string;
  videosUploaded: any[]; // Define VideoActivityDto if needed
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

export interface PriorityAlertDto {
  alertType: string; // 'ExpiringOffer' | 'UrgentApproval'
  message: string;
  studentId: number;
  studentName: string;
  deadline: Date;
  actionUrl: string;
  severity: 'High' | 'Medium' | 'Low';
}

export interface GuardianRecentActivityDto {
  title: string;
  description: string;
  timestamp: Date;
  iconType: string;
}

export interface GuardianApprovalDto {
  offerId: number;
  approved: boolean;
  notes?: string;
}

