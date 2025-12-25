import { inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  GuardianDto,
  GuardianDashboardDto,
  LinkStudentDto,
  GuardianActivityDto,
  StudentGuardianDto,
  ContactRequestAction,
  ScholarshipAction
} from '../../../core/models/guardian.models';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { ToastService } from '../../../core/services/toast.service';

@Injectable({
  providedIn: 'root'
})
export class GuardianService {
  private readonly endpoint = 'Guardian';
private http = inject(HttpClient);
  private toast = inject(ToastService);
  private apiUrl = `${environment.apiUrl}/Guardian`;
  private hubUrl = `${environment.apiUrl}/hubs/notifications`;
  
  private hubConnection?: HubConnection;
  public dashboard = signal<GuardianDashboardDto | null>(null);
  
  constructor(private api: ApiService) {}

  /**
   * Get guardian profile
   */
  getProfile(): Observable<GuardianDto> {
    return this.api.get<GuardianDto>(`${this.endpoint}/profile`);
  }

  /**
   * Update guardian profile
   */
  updateProfile(dto: Partial<GuardianDto>): Observable<any> {
    return this.api.put(`${this.endpoint}/profile`, dto);
  }

  /**
   * Get guardian dashboard
   */

  getDashboard(): Observable<GuardianDashboardDto> {
    return this.http.get<GuardianDashboardDto>(`${this.endpoint}/dashboard`).pipe(
      tap(data => this.dashboard.set(data))
    );
  }

  /**
   * Get linked students
   */
  getLinkedStudents(): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/students`);
  }

  /**
   * Link a student to guardian
   */
  linkStudent(dto: LinkStudentDto): Observable<any> {
    return this.api.post(`${this.endpoint}/link-student`, dto);
  }

  /**
   * Unlink a student from guardian
   */
  unlinkStudent(studentId: number): Observable<any> {
    return this.api.delete(`${this.endpoint}/unlink-student/${studentId}`);
  }

  /**
   * Get pending approvals (Contact Requests)
   * Route: GET /api/Guardian/contact-requests?status=Pending
   */
  getPendingApprovals(): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/contact-requests`, { status: 'Pending' });
  }

  // =========================================================
  // SCHOLARSHIPS
  // =========================================================

  /**
   * Get student's offers (for linked students only)
   * Updated to match Backend: GET /api/Guardian/scholarships?studentId={id}
   */
  getStudentOffers(studentId: number): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/scholarships`, { studentId });
  }

  /**
   * Respond to scholarship offer (Accept/Decline)
   * Updated to match Backend: PUT /api/Guardian/scholarships/{id}/respond
   */
 respondToScholarship(action: ScholarshipAction): Observable<void> {
    return this.http.post<void>(`${this.endpoint}/scholarships/${action.offerId}/respond`, action).pipe(
      tap(() => {
        this.toast.success(`Scholarship ${action.status}`);
        this.refreshDashboard();
      })
    );
  }

  // =========================================================
  // CONTACT REQUESTS
  // =========================================================

  /**
   * Get contact requests
   * Backend: GET /api/Guardian/contact-requests
   */
  getContactRequests(studentId?: number, status?: string): Observable<any[]> {
    const params: any = {};
    if (studentId) params.studentId = studentId;
    if (status) params.status = status;
    
    return this.api.get<any[]>(`${this.endpoint}/contact-requests`, params);
  }

  /**
   * Approve a contact request
   * Backend: PUT /api/Guardian/contact-requests/{id}/approve
   */

  approveContactRequest(action: ContactRequestAction): Observable<void> {
    return this.http.post<void>(`${this.endpoint}/approvals/contact`, action).pipe(
      tap(() => {
        this.toast.success(`Request ${action.approved ? 'Approved' : 'Declined'}`);
        this.refreshDashboard(); 
      })
    );
  }

  /**
   * Decline a contact request
   * Backend: PUT /api/Guardian/contact-requests/{id}/decline
   */
  declineContactRequest(requestId: number, reason?: string): Observable<any> {
    return this.api.put(`${this.endpoint}/contact-requests/${requestId}/decline`, { reason });
  }

  // =========================================================
  // STUDENT ACTIVITY & DATA
  // =========================================================

  /**
   * Get recent activity
   */
  getRecentActivity(days: number = 7): Observable<GuardianActivityDto[]> {
    return this.api.get<GuardianActivityDto[]>(`${this.endpoint}/activity`, { days });
  }

  /**
   * Get student's activity (for linked students only)
   */
  getStudentActivity(studentId: number, daysBack: number = 30): Observable<any> {
    return this.api.get<any>(`${this.endpoint}/student/${studentId}/activity`, { daysBack });
  }

  /**
   * Get student's profile/details view
   */
  getStudentProfile(studentId: number): Observable<any> {
    return this.api.get<any>(`${this.endpoint}/student/${studentId}/profile`);
  }

  // Note: These endpoints were removed/consolidated in your Controller. 
  // If they don't exist in GuardianController.cs, you might need to remove them 
  // or point them to the correct specific controller (e.g., StudentController).
  // Keeping them commented out if they are no longer in GuardianController:
  /*
  getStudentVideos(studentId: number): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/student/${studentId}/videos`);
  }
  
  getStudentRatings(studentId: number): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/student/${studentId}/ratings`);
  }
  */

  /**
   * Get guardian notifications
   */
  getNotifications(filter: any = {}): Observable<any> {
    return this.api.get<any>(`${this.endpoint}/notifications`, filter);
  }

  /**
   * Update notification preferences
   * Updated to match Backend: PUT /api/Guardian/preferences
   */
  updateNotificationPreferences(preferences: any): Observable<any> {
    return this.api.put(`${this.endpoint}/preferences`, preferences);
  }

  /**
   * Get student's guardians (for verification)
   * Note: This endpoint does not appear in your provided Controller. 
   * Ensure it exists or this call will fail.
   */
  getStudentGuardians(studentId: number): Observable<StudentGuardianDto[]> {
    return this.api.get<StudentGuardianDto[]>(`${this.endpoint}/student/${studentId}/guardians`);
  }

  /**
   * Request access to student (if not already linked)
   * Route: POST /api/Guardian/request-access
   */
  requestStudentAccess(studentEmail: string, relationship: string): Observable<any> {
    return this.api.post(`${this.endpoint}/request-access`, {
      studentEmail,
      relationship
    });
  }

  /**
   * Get guardian statistics (Dashboard summary)
   */
  getGuardianStats(): Observable<any> {
    return this.api.get(`${this.endpoint}/dashboard`); // Mapped to dashboard as stats are likely there
  }

  startSignalR() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .then(() => console.log('Guardian Hub Connected'))
      .catch((err: any) => console.error('SignalR Error:', err));

    this.hubConnection.on('DashboardUpdate', () => {
      this.toast.info('New activity received');
      this.refreshDashboard();
    });
  }

  private refreshDashboard() {
    this.getDashboard().subscribe();
  }
}