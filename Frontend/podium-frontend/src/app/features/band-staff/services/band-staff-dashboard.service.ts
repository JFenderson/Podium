// band-staff-dashboard.service.ts
// src/app/features/band-staff-dashboard/band-staff-dashboard.service.ts

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import {
  BandStaffDashboard,
  PersonalMetrics,
  MyStudent,
  StaffPerformance,
  MyActivity,
  PendingTask,
  QuickStats,
  DashboardFilters
} from '../../../core/models/band-staff-dashboard.models';

@Injectable({
  providedIn: 'root'
})
export class BandStaffDashboardService {
  private readonly apiUrl = `${environment.apiUrl}/BandStaff`;

  constructor(private http: HttpClient) {}

  getDashboard(filters?: DashboardFilters): Observable<BandStaffDashboard> {
    let params = new HttpParams();
    
    if (filters?.startDate) {
      params = params.set('startDate', filters.startDate);
    }
    if (filters?.endDate) {
      params = params.set('endDate', filters.endDate);
    }
    if (filters?.instrument) {
      params = params.set('instrument', filters.instrument);
    }
    if (filters?.contactStatus) {
      params = params.set('contactStatus', filters.contactStatus);
    }

    return this.http.get<BandStaffDashboard>(`${this.apiUrl}/dashboard`, { params });
  }

  getMetrics(startDate?: string, endDate?: string): Observable<PersonalMetrics> {
    let params = new HttpParams();
    
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);

    return this.http.get<PersonalMetrics>(`${this.apiUrl}/metrics`, { params });
  }

  getMyStudents(status?: string): Observable<MyStudent[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);

    return this.http.get<MyStudent[]>(`${this.apiUrl}/my-students`, { params });
  }

  getPerformance(startDate?: string, endDate?: string): Observable<StaffPerformance> {
    let params = new HttpParams();
    
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);

    return this.http.get<StaffPerformance>(`${this.apiUrl}/performance`, { params });
  }

  getActivity(limit: number = 20): Observable<MyActivity[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<MyActivity[]>(`${this.apiUrl}/activity`, { params });
  }

  getTasks(): Observable<PendingTask[]> {
    return this.http.get<PendingTask[]>(`${this.apiUrl}/tasks`);
  }

  getQuickStats(): Observable<QuickStats> {
    return this.http.get<QuickStats>(`${this.apiUrl}/quick-stats`);
  }

  searchStudents(searchTerm?: string, page: number = 1, pageSize: number = 20): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<any>(`${this.apiUrl}/search-students`, { params });
  }
}