// Director DTOs matching backend

export interface DirectorDto {
  directorId: number;
  applicationUserId: string;
  firstName: string;
  lastName: string;
  email?: string;
  phoneNumber?: string;
  bandId: number;
  bandName?: string;
  canManageStaff: boolean;
  canManageBand: boolean;
  createdAt: Date;
  updatedAt?: Date;
}

export interface DirectorDashboardDto {
  totalStudents: number;
  totalOffers: number;
  pendingOffers: number;
  acceptedOffers: number;
  declinedOffers: number;
  totalStaff: number;
  recentActivity: DirectorActivityDto[];
  offersByStatus: OfferStatusCount[];
  studentsByInstrument: InstrumentCount[];
  topRatedStudents: TopStudentDto[];
}

export interface DirectorActivityDto {
  activityId: string;
  type: DirectorActivityType;
  description: string;
  performedBy: string;
  timestamp: Date;
  relatedEntityId?: number;
}

export enum DirectorActivityType {
  OfferCreated = 'OfferCreated',
  OfferSent = 'OfferSent',
  OfferAccepted = 'OfferAccepted',
  OfferDeclined = 'OfferDeclined',
  StudentRated = 'StudentRated',
  StaffAdded = 'StaffAdded',
  StaffUpdated = 'StaffUpdated',
  EventCreated = 'EventCreated',
  ContactRequested = 'ContactRequested'
}

export interface OfferStatusCount {
  status: string;
  count: number;
}

export interface InstrumentCount {
  instrument: string;
  count: number;
}

export interface TopStudentDto {
  studentId: number;
  firstName: string;
  lastName: string;
  primaryInstrument?: string;
  averageRating: number;
  ratingCount: number;
}

export interface BandStatisticsDto {
  totalMembers: number;
  averageGpa?: number;
  totalScholarshipOffered: number;
  acceptanceRate: number;
  instrumentDistribution: InstrumentDistributionDto[];
  studentsByState: StateDistributionDto[];
  studentsByGraduationYear: GraduationYearDistributionDto[];
}

export interface InstrumentDistributionDto {
  instrument: string;
  count: number;
  percentage: number;
}

export interface StateDistributionDto {
  state: string;
  count: number;
}

export interface GraduationYearDistributionDto {
  year: number;
  count: number;
}