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
  firstName: string;
  lastName: string;
  email?: string;
  graduationYear?: number;
  highSchool?: string;
  primaryInstrument?: string;
  pendingOffers: number;
  pendingApprovals: number;
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


