
export interface ScholarshipOfferDto {
  offerId: number;
  studentId: number;
  studentName?: string;
  bandId: number;
  bandName?: string;
  offerType: OfferType;

  amount?: number;
  description: string;
  terms?: string;
  status: ScholarshipOfferStatus;
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
  terms?: string;
}

export interface UpdateScholarshipOfferDto {
  amount?: number;
  description?: string;
  expiresAt?: Date;
  status?: ScholarshipOfferStatus;
}

export interface RespondToScholarshipOfferDto {
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

export enum ScholarshipOfferStatus {
  Draft = 'Draft',
  Sent = 'Sent',
  Viewed = 'Viewed',
  Accepted = 'Accepted',
  Declined = 'Declined',
  Expired = 'Expired',
  Withdrawn = 'Withdrawn',
  PendingGuardianSignature = 'PendingGuardianSignature'
}

export enum ApprovalStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Declined = 'Declined'
}

export interface ScholarshipFilterDto {
  studentId?: number;
  bandId?: number;
  status?: ScholarshipOfferStatus;
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
  status: ScholarshipOfferStatus;
  sentAt?: Date;
  expiresAt?: Date;
}

