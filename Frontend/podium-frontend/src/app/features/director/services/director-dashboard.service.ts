// director-dashboard.service.ts
// Frontend/podium-frontend/src/app/features/director/services/director-dashboard.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../features/auth/services/auth.service';
import {
  DirectorDashboardDto,
  DirectorDashboardFilters,
  PendingApprovalDto,
  StaffPerformanceDto,
  DashboardUpdate,
  ExportOptions
} from '../../../core/models/director-dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class DirectorDashboardService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  
  private readonly apiUrl = `${environment.apiUrl}/Director`;
  private readonly hubUrl = environment.apiUrl.replace('/api', '') + '/directorHub';
  
  private hubConnection?: HubConnection;
  
  // Real-time updates subject
  private dashboardUpdates$ = new BehaviorSubject<DashboardUpdate | null>(null);
  public updates$ = this.dashboardUpdates$.asObservable();
  
  // Metrics cache for quick updates
  private metricsCache$ = new BehaviorSubject<any>(null);
  public cachedMetrics$ = this.metricsCache$.asObservable();

  constructor() {}

  // ============================================
  // DASHBOARD DATA
  // ============================================

  /**
   * Get complete dashboard data
   */
  getDashboard(filters?: DirectorDashboardFilters): Observable<DirectorDashboardDto> {
    let params = new HttpParams();
    
    if (filters?.dateRangeStart) {
      params = params.set('startDate', filters.dateRangeStart.toISOString());
    }
    
    if (filters?.dateRangeEnd) {
      params = params.set('endDate', filters.dateRangeEnd.toISOString());
    }
    
    if (filters?.recruiterId) {
      params = params.set('recruiterId', filters.recruiterId.toString());
    }
    
    if (filters?.instrument) {
      params = params.set('instrument', filters.instrument);
    }
    
    if (filters?.offerStatus) {
      params = params.set('offerStatus', filters.offerStatus);
    }

    return this.http.get<DirectorDashboardDto>(`${this.apiUrl}/dashboard`, { params }).pipe(
      tap(data => this.metricsCache$.next(data.keyMetrics))
    );
  }

  /**
   * Get key metrics only (lightweight)
   */
  getKeyMetrics(startDate?: Date, endDate?: Date): Observable<any> {
    let params = new HttpParams();
    
    if (startDate) {
      params = params.set('startDate', startDate.toISOString());
    }
    
    if (endDate) {
      params = params.set('endDate', endDate.toISOString());
    }

    return this.http.get(`${this.apiUrl}/metrics`, { params });
  }

  /**
   * Get recruitment funnel data
   */
  getRecruitmentFunnel(filters?: DirectorDashboardFilters): Observable<any> {
    let params = new HttpParams();
    
    if (filters?.dateRangeStart) {
      params = params.set('startDate', filters.dateRangeStart.toISOString());
    }
    
    if (filters?.dateRangeEnd) {
      params = params.set('endDate', filters.dateRangeEnd.toISOString());
    }

    return this.http.get(`${this.apiUrl}/funnel`, { params });
  }

  /**
   * Get offers overview with time series
   */
  getOffersOverview(filters?: DirectorDashboardFilters): Observable<any> {
    let params = new HttpParams();
    
    if (filters?.dateRangeStart) {
      params = params.set('startDate', filters.dateRangeStart.toISOString());
    }
    
    if (filters?.dateRangeEnd) {
      params = params.set('endDate', filters.dateRangeEnd.toISOString());
    }

    return this.http.get(`${this.apiUrl}/offers-overview`, { params });
  }

  /**
   * Get staff performance data
   */
  getStaffPerformance(filters?: DirectorDashboardFilters): Observable<StaffPerformanceDto[]> {
    let params = new HttpParams();
    
    if (filters?.dateRangeStart) {
      params = params.set('startDate', filters.dateRangeStart.toISOString());
    }
    
    if (filters?.dateRangeEnd) {
      params = params.set('endDate', filters.dateRangeEnd.toISOString());
    }
    
    if (filters?.sortBy) {
      params = params.set('sortBy', filters.sortBy);
    }
    
    if (filters?.sortDirection) {
      params = params.set('sortDirection', filters.sortDirection);
    }

    return this.http.get<StaffPerformanceDto[]>(`${this.apiUrl}/staff-performance`, { params });
  }

  // ============================================
  // APPROVALS
  // ============================================

  /**
   * Get pending approvals
   */
  getPendingApprovals(): Observable<PendingApprovalDto[]> {
    return this.http.get<PendingApprovalDto[]>(`${this.apiUrl}/pending-approvals`);
  }

  /**
   * Approve scholarship offer
   */
  approveOffer(approvalId: number, notes?: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/approvals/${approvalId}/approve`, { notes });
  }

  /**
   * Deny scholarship offer
   */
  denyOffer(approvalId: number, reason: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/approvals/${approvalId}/deny`, { reason });
  }

  // ============================================
  // STAFF MANAGEMENT
  // ============================================

  /**
   * Update staff budget
   */
  updateStaffBudget(staffId: number, newBudget: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/staff/${staffId}/budget`, { budget: newBudget });
  }

  /**
   * Update staff permissions
   */
  updateStaffPermissions(staffId: number, permissions: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/staff/${staffId}/permissions`, permissions);
  }

  /**
   * Get staff details
   */
  getStaffDetails(staffId: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/staff/${staffId}`);
  }

  // ============================================
  // ACTIVITY FEED
  // ============================================

  /**
   * Get recent activity
   */
  getRecentActivity(limit: number = 20): Observable<any[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<any[]>(`${this.apiUrl}/activity`, { params });
  }

  // ============================================
  // EXPORT
  // ============================================

  /**
   * Export dashboard data
   */
  exportDashboard(options: ExportOptions): Observable<Blob> {
    const params = new HttpParams()
      .set('format', options.format)
      .set('includeCharts', options.includeCharts.toString())
      .set('startDate', options.dateRange.start.toISOString())
      .set('endDate', options.dateRange.end.toISOString())
      .set('sections', options.sections.join(','));

    return this.http.get(`${this.apiUrl}/export`, {
      params,
      responseType: 'blob'
    });
  }

  /**
   * Download export file
   */
  downloadExport(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    window.URL.revokeObjectURL(url);
  }

  // ============================================
  // SIGNALR REAL-TIME UPDATES
  // ============================================

  /**
   * Start SignalR connection
   */
  async startSignalR(): Promise<void> {
    if (this.hubConnection) {
      return;
    }

    const token = this.authService.getToken();
    
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => token || ''
      })
      .configureLogging(LogLevel.Warning)
      .withAutomaticReconnect()
      .build();

    // Handle new activity
    this.hubConnection.on('NewActivity', (activity: any) => {
      this.dashboardUpdates$.next({
        type: 'NewActivity',
        data: activity,
        timestamp: new Date()
      });
    });

    // Handle metric updates
    this.hubConnection.on('MetricUpdate', (metrics: any) => {
      this.dashboardUpdates$.next({
        type: 'MetricUpdate',
        data: metrics,
        timestamp: new Date()
      });
      this.metricsCache$.next(metrics);
    });

    // Handle new approvals
    this.hubConnection.on('ApprovalNeeded', (approval: any) => {
      this.dashboardUpdates$.next({
        type: 'ApprovalNeeded',
        data: approval,
        timestamp: new Date()
      });
    });

    // Handle staff updates
    this.hubConnection.on('StaffUpdate', (staffUpdate: any) => {
      this.dashboardUpdates$.next({
        type: 'StaffUpdate',
        data: staffUpdate,
        timestamp: new Date()
      });
    });

    try {
      await this.hubConnection.start();
      console.log('Director SignalR connected');
    } catch (error) {
      console.error('SignalR connection error:', error);
      // Retry after 5 seconds
      setTimeout(() => this.startSignalR(), 5000);
    }
  }

  /**
   * Stop SignalR connection
   */
  async stopSignalR(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = undefined;
    }
  }

  // ============================================
  // UTILITY
  // ============================================

  /**
   * Get funnel stage students
   */
  getFunnelStageStudents(stage: string, filters?: DirectorDashboardFilters): Observable<any[]> {
    let params = new HttpParams().set('stage', stage);
    
    if (filters?.dateRangeStart) {
      params = params.set('startDate', filters.dateRangeStart.toISOString());
    }
    
    if (filters?.dateRangeEnd) {
      params = params.set('endDate', filters.dateRangeEnd.toISOString());
    }

    return this.http.get<any[]>(`${this.apiUrl}/funnel/${stage}/students`, { params });
  }

  /**
   * Calculate percentage change
   */
  calculatePercentageChange(current: number, previous: number): number {
    if (previous === 0) return 100;
    return ((current - previous) / previous) * 100;
  }

  /**
   * Format currency
   */
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(amount);
  }

  /**
   * Format percentage
   */
  formatPercentage(value: number, decimals: number = 1): string {
    return `${value.toFixed(decimals)}%`;
  }
}