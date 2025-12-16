export interface BandStaffDto {
  bandStaffId: number;
  bandId: number;
  firstName: string;
  lastName: string;
  role: string;
  // Metrics
  totalContactsInitiated: number;
  totalOffersCreated: number;
  successfulPlacements: number;
  // Permissions
  canViewStudents: boolean;
  canRateStudents: boolean;
  canSendOffers: boolean;
  canManageEvents: boolean;
}