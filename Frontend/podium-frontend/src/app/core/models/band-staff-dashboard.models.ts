// band-staff-dashboard.models.ts
// src/app/features/band-staff-dashboard/models/band-staff-dashboard.models.ts

export interface BandStaffDashboard {
  personalMetrics: PersonalMetrics;
  myStudents: MyStudent[];
  performance: StaffPerformance;
  recentActivity: MyActivity[];
  pendingTasks: PendingTask[];
  dateRangeStart: string;
  dateRangeEnd: string;
}

export interface PersonalMetrics {
  offersCreated: number;
  offersAccepted: number;
  acceptanceRate: number;
  acceptanceRateChange: number;
  studentsContacted: number;
  studentsResponded: number;
  responseRate: number;
  responseRateChange: number;
  budgetAllocated: number;
  budgetUsed: number;
  budgetRemaining: number;
  budgetUtilization: number;
  daysSinceLastActivity: number;
  ratingsGiven: number;
  averageOfferAmount: number;
  myRankByOffers?: number;
  myRankByAcceptance?: number;
  totalStaff: number;
}

export interface MyStudent {
  studentId: number;
  firstName: string;
  lastName: string;
  fullName: string;
  profilePhotoUrl?: string;
  primaryInstrument: string;
  state?: string;
  graduationYear: number;
  gpa?: number;
  contactedDate?: string;
  contactStatus: string;
  offerSentDate?: string;
  offerAmount?: number;
  offerStatus?: string;
  myRating?: number;
  lastRatedDate?: string;
  videoCount: number;
  averageRating?: number;
  totalRatings: number;
  lastActivityDate?: string;
  canContact: boolean;
  canMakeOffer: boolean;
  canRate: boolean;
}

export interface StaffPerformance {
  studentsContacted: number;
  studentsInterested: number;
  offersExtended: number;
  offersAccepted: number;
  studentsEnrolled: number;
  contactToInterestRate: number;
  interestToOfferRate: number;
  offerToAcceptanceRate: number;
  overallConversionRate: number;
  monthlyMetrics: PerformanceTimeSeries[];
  myAcceptanceRate: number;
  teamAverageAcceptanceRate: number;
  myResponseRate: number;
  teamAverageResponseRate: number;
}

export interface PerformanceTimeSeries {
  month: string;
  date: string;
  offersCreated: number;
  offersAccepted: number;
  contactsMade: number;
  responsesReceived: number;
}

export interface MyActivity {
  id: number;
  timestamp: string;
  activityType: string;
  description: string;
  studentId?: number;
  studentName?: string;
  details?: string;
  icon?: string;
  color?: string;
}

export interface PendingTask {
  id: number;
  taskType: string;
  title: string;
  description: string;
  dueDate: string;
  priority: string;
  studentId?: number;
  studentName?: string;
  canComplete: boolean;
}

export interface QuickStats {
  activeStudents: number;
  pendingContacts: number;
  pendingOffers: number;
  budgetRemaining: number;
}

export interface DashboardFilters {
  startDate?: string;
  endDate?: string;
  instrument?: string;
  contactStatus?: string;
}