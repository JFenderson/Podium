import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api';
import {
  GuardianDto,
  GuardianDashboardDto,
  GuardianApprovalDto,
  GuardianPendingApprovalDto,
  LinkStudentDto,
  GuardianActivityDto,
  StudentGuardianDto
} from '../../../core/models/guardian';

@Injectable({
  providedIn: 'root'
})
export class GuardianService {
  private readonly endpoint = 'Guardian';

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
    return this.api.get<GuardianDashboardDto>(`${this.endpoint}/dashboard`);
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
   * Get pending approvals
   */
  getPendingApprovals(): Observable<GuardianPendingApprovalDto[]> {
    return this.api.get<GuardianPendingApprovalDto[]>(`${this.endpoint}/pending-approvals`);
  }

  /**
   * Approve or deny scholarship offer
   */
  approveOffer(offerId: number, dto: GuardianApprovalDto): Observable<any> {
    return this.api.post(`${this.endpoint}/approve-offer/${offerId}`, dto);
  }

  /**
   * Get recent activity
   */
  getRecentActivity(days: number = 7): Observable<GuardianActivityDto[]> {
    return this.api.get<GuardianActivityDto[]>(`${this.endpoint}/activity`, { days });
  }

  /**
   * Get student's offers (for linked students only)
   */
  getStudentOffers(studentId: number): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/student/${studentId}/offers`);
  }

  /**
   * Get student's activity (for linked students only)
   */
  getStudentActivity(studentId: number): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/student/${studentId}/activity`);
  }

  /**
   * Get student's videos (for linked students only)
   */
  getStudentVideos(studentId: number): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/student/${studentId}/videos`);
  }

  /**
   * Get student's ratings (for linked students only)
   */
  getStudentRatings(studentId: number): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/student/${studentId}/ratings`);
  }

  /**
   * Send message to student
   */
  sendMessageToStudent(studentId: number, message: string): Observable<any> {
    return this.api.post(`${this.endpoint}/student/${studentId}/message`, { message });
  }

  /**
   * Get guardian notifications
   */
  getNotifications(): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/notifications`);
  }

  /**
   * Get approval history
   */
  getApprovalHistory(): Observable<any[]> {
    return this.api.get<any[]>(`${this.endpoint}/approval-history`);
  }

  /**
   * Request access to student (if not already linked)
   */
  requestStudentAccess(studentEmail: string, relationship: string): Observable<any> {
    return this.api.post(`${this.endpoint}/request-access`, {
      studentEmail,
      relationship
    });
  }

  /**
   * Get student's guardians (for verification)
   */
  getStudentGuardians(studentId: number): Observable<StudentGuardianDto[]> {
    return this.api.get<StudentGuardianDto[]>(`${this.endpoint}/student/${studentId}/guardians`);
  }

  /**
   * Update notification preferences
   */
  updateNotificationPreferences(preferences: any): Observable<any> {
    return this.api.put(`${this.endpoint}/notification-preferences`, preferences);
  }

  /**
   * Get guardian statistics
   */
  getGuardianStats(): Observable<any> {
    return this.api.get(`${this.endpoint}/stats`);
  }
}