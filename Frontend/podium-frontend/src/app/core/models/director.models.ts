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

export interface OfferStatsDto {
  totalOffers: number;
  pending: number;
  accepted: number;
  declined: number;
  expired: number;
  acceptanceRate: number;
  responseRate: number;
}

export interface DirectorEngagementMetricsDto {
  totalProfileViews: number;
  totalVideoWatches: number;
  totalInterests: number;
  dailyActivity: DailyEngagementDto[];
}

export interface DailyEngagementDto {
  date: Date;
  views: number;
  interests: number;
}

export interface RecruiterPerformanceDto {
  staffId: number;
  name: string;
  contactsInitiated: number;
  offersSent: number;
  successfulPlacements: number;
  conversionRate: number;
}

export interface BandBudgetDto {
  totalBudget: number;
  allocated: number;         // Accepted offers
  remaining: number;
  pendingCommitment: number; // Sent/Pending offers
  fiscalYear: number;
}

export interface ConversionFunnelDto {
  totalInterests: number;
  contacted: number;
  offersSent: number;
  offersAccepted: number;
  interestToContactRate: number;
  contactToOfferRate: number;
  offerToAcceptRate: number;
}

// Wrapper for the view state
export interface AnalyticsDashboardState {
  offerStats: OfferStatsDto | null;
  engagement: DirectorEngagementMetricsDto | null;
  staffPerformance: RecruiterPerformanceDto[];
  budget: BandBudgetDto | null;
  funnel: ConversionFunnelDto | null;
}