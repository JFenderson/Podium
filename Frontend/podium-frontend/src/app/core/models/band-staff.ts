// BandStaff DTOs matching backend

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
  updatedAt?: Date;
}

export interface CreateBandStaffDto {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  role: string;
  bandId: number;
  canViewStudents: boolean;
  canRateStudents: boolean;
  canSendOffers: boolean;
  canContactStudents: boolean;
  canManageEvents: boolean;
  canManageStaff: boolean;
}

export interface UpdateBandStaffDto {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  role?: string;
  canViewStudents?: boolean;
  canRateStudents?: boolean;
  canSendOffers?: boolean;
  canContactStudents?: boolean;
  canManageEvents?: boolean;
  canManageStaff?: boolean;
}

export interface BandStaffPermissionsDto {
  canViewStudents: boolean;
  canRateStudents: boolean;
  canSendOffers: boolean;
  canContactStudents: boolean;
  canManageEvents: boolean;
  canManageStaff: boolean;
}

export interface BandStaffSummaryDto {
  bandStaffId: number;
  firstName: string;
  lastName: string;
  role: string;
  email?: string;
  bandName?: string;
}