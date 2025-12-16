export interface GuardianDashboardDto {
  linkedStudents: StudentSummaryDto[];
  totalPendingApprovals: number;
  totalActiveOffers: number;
  totalPendingOffers: number;
  totalUnreadNotifications: number;
  priorityAlerts: PriorityAlertDto[];
  recentActivities: GuardianRecentActivityDto[];
}

export interface StudentSummaryDto {
  studentId: number;
  studentName: string;
  primaryInstrument: string;
  graduationYear: number;
  pendingContactRequests: number;
  activeScholarshipOffers: number;
}

export interface PriorityAlertDto {
  alertType: string;
  message: string;
  studentName: string;
  deadline?: string;
  severity: string; // "High", "Medium", "Low"
}

export interface GuardianRecentActivityDto {
  activityType: string;
  description: string;
  timestamp: string;
  studentName: string;
}