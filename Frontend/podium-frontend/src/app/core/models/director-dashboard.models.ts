// director-dashboard.models.ts
// Frontend/podium-frontend/src/app/core/models/director-dashboard.models.ts

export interface DirectorDashboardDto {
  // Key Metrics
  keyMetrics: DirectorKeyMetrics;
  
  // Recruitment Funnel
  recruitmentFunnel: FunnelStageDto[];
  
  // Offers Overview
  offersOverview: OffersOverviewDto;
  
  // Staff Performance
  staffPerformance: StaffPerformanceDto[];
  
  // Pending Approvals
  pendingApprovals: PendingApprovalDto[];
  
  // Recent Activity
  recentActivity: ActivityItemDto[];
  
  // Date Range
  dateRangeStart: Date;
  dateRangeEnd: Date;
}

export interface DirectorKeyMetrics {
  totalOffersSent: number;
  offersSentChange: number; // Percentage change from previous period
  
  acceptanceRate: number;
  acceptanceRateChange: number;
  
  activeRecruiters: number;
  activeRecruitersChange: number;
  
  pipelineStudents: number;
  pipelineStudentsChange: number;
  
  totalBudgetAllocated: number;
  totalBudgetUsed: number;
  budgetUtilization: number;
  
  averageOfferAmount: number;
  topInstrumentNeeds: string[];
}

export interface FunnelStageDto {
  stage: 'Contacted' | 'Interested' | 'Offered' | 'Accepted' | 'Enrolled';
  count: number;
  percentage: number;
  conversionRate?: number; // From previous stage
  students?: FunnelStudentDto[];
}

export interface FunnelStudentDto {
  studentId: number;
  studentName: string;
  instrument: string;
  currentStage: string;
  daysInStage: number;
}

export interface OffersOverviewDto {
  // Time series data
  offersByMonth: OfferTimeSeriesDto[];
  
  // Breakdown by instrument
  offersByInstrument: OfferBreakdownDto[];
  
  // Breakdown by status
  offersByStatus: OfferBreakdownDto[];
  
  // Breakdown by recruiter
  offersByRecruiter: OfferBreakdownDto[];
  
  // Summary stats
  totalOffers: number;
  acceptedOffers: number;
  pendingOffers: number;
  declinedOffers: number;
  expiredOffers: number;
}

export interface OfferTimeSeriesDto {
  date: Date;
  month: string;
  totalOffers: number;
  acceptedOffers: number;
  declinedOffers: number;
  averageAmount: number;
}

export interface OfferBreakdownDto {
  label: string;
  count: number;
  percentage: number;
  totalAmount?: number;
  averageAmount?: number;
}

export interface StaffPerformanceDto {
  staffId: number;
  staffName: string;
  role: string;
  email: string;
  
  // Performance Metrics
  offersCreated: number;
  offersAccepted: number;
  acceptanceRate: number;
  
  studentsContacted: number;
  studentsResponded: number;
  responseRate: number;
  
  totalBudgetAllocated: number;
  averageOfferAmount: number;
  
  lastActivityDate: Date;
  daysActive: number;
  
  // Rankings
  performanceRank?: number;
  acceptanceRateRank?: number;
}

export interface PendingApprovalDto {
  approvalId: number;
  type: 'ScholarshipOffer' | 'BudgetIncrease' | 'StaffPermission';
  
  // Scholarship specific
  studentId?: number;
  studentName?: string;
  instrument?: string;
  
  // Offer details
  amount?: number;
  offerType?: string;
  description?: string;
  
  // Staff details
  requestedByStaffId: number;
  requestedByStaffName: string;
  
  // Approval details
  requestDate: Date;
  urgency: 'Low' | 'Medium' | 'High';
  reason?: string;
  
  // Actions
  canApprove: boolean;
  canDeny: boolean;
}

export interface ActivityItemDto {
  id: number;
  timestamp: Date;
  activityType: 'OfferSent' | 'OfferAccepted' | 'OfferDeclined' | 'ContactMade' | 'VideoUploaded' | 'InterestShown' | 'StaffAction';
  
  // Primary actor
  actorType: 'Student' | 'Staff' | 'System';
  actorId?: number;
  actorName?: string;
  
  // Related entities
  studentId?: number;
  studentName?: string;
  
  staffId?: number;
  staffName?: string;
  
  offerId?: number;
  
  // Activity details
  description: string;
  details?: string;
  metadata?: any;
  
  // UI
  icon?: string;
  color?: string;
}

// Filters and Options
export interface DirectorDashboardFilters {
  dateRangeStart?: Date;
  dateRangeEnd?: Date;
  recruiterId?: number;
  instrument?: string;
  offerStatus?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface DateRange {
  start: Date;
  end: Date;
  label: string;
}

export const PREDEFINED_DATE_RANGES: DateRange[] = [
  {
    start: new Date(new Date().setDate(new Date().getDate() - 7)),
    end: new Date(),
    label: 'Last 7 Days'
  },
  {
    start: new Date(new Date().setDate(new Date().getDate() - 30)),
    end: new Date(),
    label: 'Last 30 Days'
  },
  {
    start: new Date(new Date().setDate(new Date().getDate() - 90)),
    end: new Date(),
    label: 'Last 90 Days'
  },
  {
    start: new Date(new Date().getFullYear(), 0, 1),
    end: new Date(),
    label: 'Year to Date'
  },
  {
    start: new Date(new Date().getFullYear() - 1, 0, 1),
    end: new Date(new Date().getFullYear() - 1, 11, 31),
    label: 'Last Year'
  }
];

// Chart Configuration
export interface ChartConfig {
  type: 'line' | 'bar' | 'pie' | 'doughnut' | 'funnel';
  data: any;
  options: any;
}

// Export formats
export type ExportFormat = 'csv' | 'excel' | 'pdf';

export interface ExportOptions {
  format: ExportFormat;
  includeCharts: boolean;
  dateRange: DateRange;
  sections: string[];
}

// Real-time update
export interface DashboardUpdate {
  type: 'NewActivity' | 'MetricUpdate' | 'ApprovalNeeded' | 'StaffUpdate';
  data: any;
  timestamp: Date;
}

// Staff Management Quick Actions
export interface StaffQuickAction {
  action: 'ViewProfile' | 'SendMessage' | 'AdjustBudget' | 'ManagePermissions' | 'ViewOffers';
  staffId: number;
  staffName: string;
}

// Funnel Stage Colors
export const FUNNEL_STAGE_COLORS: Record<string, string> = {
  'Contacted': '#3B82F6',    // blue-500
  'Interested': '#8B5CF6',   // purple-500
  'Offered': '#F59E0B',      // amber-500
  'Accepted': '#10B981',     // green-500
  'Enrolled': '#059669'      // green-600
};

// Activity Type Icons & Colors
export const ACTIVITY_CONFIG: Record<string, { icon: string; color: string }> = {
  'OfferSent': { icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'blue' },
  'OfferAccepted': { icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'green' },
  'OfferDeclined': { icon: 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z', color: 'red' },
  'ContactMade': { icon: 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z', color: 'purple' },
  'VideoUploaded': { icon: 'M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z', color: 'indigo' },
  'InterestShown': { icon: 'M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z', color: 'pink' },
  'StaffAction': { icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z', color: 'gray' }
};