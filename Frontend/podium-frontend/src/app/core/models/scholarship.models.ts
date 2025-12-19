// Scholarship DTOs matching backend

export interface ScholarshipOfferDto {
  offerId: number;
  studentId: number;
  studentName?: string;
  bandId: number;
  bandName?: string;
  offerType: OfferType;
  amount?: number;
  description: string;
  status: OfferStatus;
  expiresAt?: Date;
  sentAt?: Date;
  respondedAt?: Date;
  studentNotes?: string;
  requiresGuardianApproval: boolean;
  guardianApprovalStatus?: ApprovalStatus;
  createdByUserId: string;
  createdByName?: string;
  createdAt: Date;
  updatedAt: Date;
  duration?: string;
}

export interface CreateScholarshipOfferDto {
  studentId: number;
  bandId: number;
  offerType: OfferType;
  amount?: number;
  description: string;
  expiresAt?: string | Date;
  requiresGuardianApproval: boolean;
}

export interface UpdateScholarshipOfferDto {
  amount?: number;
  description?: string;
  expiresAt?: Date;
  status?: OfferStatus;
}

export interface RespondToOfferDto {
  isAccepted: boolean;
  notes?: string;
}

export interface GuardianApprovalDto {
  approved: boolean;
  notes?: string;
}

export enum OfferType {
  Scholarship = 'Scholarship',
  SpotOffer = 'SpotOffer',
  Interest = 'Interest'
}

export enum OfferStatus {
  Draft = 'Draft',
  Sent = 'Sent',
  Viewed = 'Viewed',
  Accepted = 'Accepted',
  Declined = 'Declined',
  Expired = 'Expired',
  Withdrawn = 'Withdrawn'
}

export enum ApprovalStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Declined = 'Declined'
}

export interface ScholarshipFilterDto {
  studentId?: number;
  bandId?: number;
  status?: OfferStatus;
  offerType?: OfferType;
  pageNumber?: number;
  pageSize?: number;
}

export interface ScholarshipSummaryDto {
  offerId: number;
  studentName: string;
  bandName: string;
  offerType: OfferType;
  amount?: number;
  status: OfferStatus;
  sentAt?: Date;
  expiresAt?: Date;
}

