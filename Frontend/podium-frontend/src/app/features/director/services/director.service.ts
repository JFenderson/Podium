import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  DirectorDto,
  DirectorDashboardDto,
  BandStatisticsDto,
  DirectorActivityDto
} from '../../../core/models/director.models';

@Injectable({
  providedIn: 'root'
})
export class DirectorService {
  private readonly endpoint = 'Director';

  constructor(private api: ApiService) {}

  /**
   * Get director profile
   */
  getProfile(): Observable<DirectorDto> {
    return this.api.get<DirectorDto>(`${this.endpoint}/profile`);
  }

  /**
   * Get director dashboard data
   */
  getDashboard(): Observable<DirectorDashboardDto> {
    return this.api.get<DirectorDashboardDto>(`${this.endpoint}/dashboard`);
  }

  /**
   * Get band statistics
   */
  getBandStatistics(bandId: number): Observable<BandStatisticsDto> {
    return this.api.get<BandStatisticsDto>(`${this.endpoint}/band/${bandId}/statistics`);
  }

  /**
   * Get recent activity
   */
  getRecentActivity(days: number = 7): Observable<DirectorActivityDto[]> {
    return this.api.get<DirectorActivityDto[]>(`${this.endpoint}/activity`, { days });
  }

  /**
   * Get recruitment analytics
   */
  getRecruitmentAnalytics(bandId: number, dateFrom?: Date, dateTo?: Date): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/analytics`, {
      dateFrom: dateFrom?.toISOString(),
      dateTo: dateTo?.toISOString()
    });
  }

  /**
   * Get offer statistics by status
   */
  getOfferStatistics(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/offer-stats`);
  }

  /**
   * Get student engagement metrics
   */
  getStudentEngagement(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/engagement`);
  }

  /**
   * Get staff performance metrics
   */
  getStaffPerformance(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/staff-performance`);
  }

  /**
   * Export dashboard data
   */
  exportDashboardData(bandId: number, format: 'csv' | 'excel' | 'pdf'): Observable<Blob> {
    return this.api.download(`${this.endpoint}/band/${bandId}/export`, { format });
  }

  /**
   * Get top rated students
   */
  getTopRatedStudents(bandId: number, limit: number = 10): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/top-students`, { limit });
  }

  /**
   * Get conversion funnel data
   */
  getConversionFunnel(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/conversion-funnel`);
  }

  /**
   * Get scholarship budget analysis
   */
  getScholarshipBudget(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/scholarship-budget`);
  }

  /**
   * Get geographic distribution of students
   */
  getGeographicDistribution(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/geographic-distribution`);
  }

  /**
   * Get instrument needs analysis
   */
  getInstrumentNeeds(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/instrument-needs`);
  }

  /**
   * Schedule bulk action (e.g., send multiple offers)
   */
  scheduleBulkAction(action: any): Observable<any> {
    return this.api.post(`${this.endpoint}/bulk-action`, action);
  }

  /**
   * Get comparative analytics (vs other bands)
   */
  getComparativeAnalytics(bandId: number): Observable<any> {
    return this.api.get(`${this.endpoint}/band/${bandId}/comparative-analytics`);
  }
}