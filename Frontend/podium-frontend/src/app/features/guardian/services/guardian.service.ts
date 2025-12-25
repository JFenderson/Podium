import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { AuthService } from '../../auth/services/auth.service';
import { environment } from '../../../../../environments/environment';
import {
  GuardianDto,
  GuardianDashboardDto,
  LinkStudentDto,
  NotificationListDto,
  GuardianNotificationPreferencesDto,
  StudentGuardianDto
} from '../../../core/models/guardian.models';

@Injectable({
  providedIn: 'root'
})
export class GuardianService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  
  private readonly apiUrl = `${environment.apiUrl}/Guardian`;
  private readonly hubUrl = environment.apiUrl.replace('/api', '') + '/notificationHub';
  private hubConnection?: HubConnection;

  constructor() {}

  // --- Dashboard & Profile ---

  getDashboard(): Observable<GuardianDashboardDto> {
    return this.http.get<GuardianDashboardDto>(`${this.apiUrl}/dashboard`);
  }

  getProfile(): Observable<GuardianDto> {
    return this.http.get<GuardianDto>(`${this.apiUrl}/profile`);
  }

  updateProfile(dto: Partial<GuardianDto>): Observable<any> {
    return this.http.put(`${this.apiUrl}/profile`, dto);
  }

  // --- Students ---

  getLinkedStudents(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/students`);
  }

  linkStudent(dto: LinkStudentDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/link-student`, dto);
  }

  unlinkStudent(studentId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/unlink-student/${studentId}`);
  }

  requestStudentAccess(studentEmail: string, relationship: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/request-access`, { studentEmail, relationship });
  }

  getStudentProfile(studentId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/student/${studentId}/profile`);
  }

  getStudentActivity(studentId: number, daysBack: number = 30): Observable<any> {
    const params = new HttpParams().set('daysBack', daysBack.toString());
    return this.http.get<any>(`${this.apiUrl}/student/${studentId}/activity`, { params });
  }

  getStudentGuardians(studentId: number): Observable<StudentGuardianDto[]> {
    return this.http.get<StudentGuardianDto[]>(`${this.apiUrl}/student/${studentId}/guardians`);
  }

  // FIX: Added missing method
  getStudentOffers(studentId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/scholarships?studentId=${studentId}`);
  }

  // --- Requests & Offers ---

  getContactRequests(studentId?: number, status?: string): Observable<any[]> {
    let params = new HttpParams();
    if (studentId) params = params.set('studentId', studentId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<any[]>(`${this.apiUrl}/contact-requests`, { params });
  }

  approveContactRequest(requestId: number, notes?: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/contact-requests/${requestId}/approve`, { notes });
  }

  declineContactRequest(requestId: number, reason?: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/contact-requests/${requestId}/decline`, { reason });
  }

  getScholarships(studentId?: number, status?: string): Observable<any[]> {
    let params = new HttpParams();
    if (studentId) params = params.set('studentId', studentId.toString());
    if (status) params = params.set('status', status);
    return this.http.get<any[]>(`${this.apiUrl}/scholarships`, { params });
  }

  respondToScholarship(offerId: number, response: string, notes?: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/scholarships/${offerId}/respond`, { response, notes });
  }

  // --- Notifications ---

  getNotifications(filter: any = {}): Observable<NotificationListDto> {
    let params = new HttpParams();
    Object.keys(filter).forEach(key => {
      if (filter[key] !== null && filter[key] !== undefined) {
        params = params.append(key, filter[key].toString());
      }
    });
    return this.http.get<NotificationListDto>(`${this.apiUrl}/notifications`, { params });
  }

  updateNotificationPreferences(preferences: any): Observable<GuardianNotificationPreferencesDto> {
    return this.http.put<GuardianNotificationPreferencesDto>(`${this.apiUrl}/preferences`, preferences);
  }

  // --- SignalR ---

  startSignalR() {
    if (this.hubConnection?.state === 'Connected') return;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => {
          const token = this.authService.getToken();
          return token ? Promise.resolve(token) : Promise.reject('No token found');
        }
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('✅ Guardian SignalR Connected'))
      .catch(err => console.error('❌ SignalR Connection Error:', err));

    this.hubConnection.on('DashboardUpdate', () => {
      this.getDashboard().subscribe(); 
    });
  }
}