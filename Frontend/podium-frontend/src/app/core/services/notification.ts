import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject, interval } from 'rxjs';
import { tap, switchMap } from 'rxjs/operators';
import { ApiService } from '../../core/services/api';
import {
  NotificationDto,
  NotificationPreferencesDto,
  UpdateNotificationPreferencesDto,
  NotificationSummaryDto,
  NotificationFilterDto,
  MarkNotificationsReadDto
} from '../models/notification';
import { PagedResult } from '../../core/models/student';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly endpoint = 'Notifications';
  
  private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$ = this.unreadCountSubject.asObservable();
  
  private notificationsSubject = new BehaviorSubject<NotificationDto[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  private pollingInterval = 30000; // 30 seconds
  private polling$?: Observable<any>;

  constructor(private api: ApiService) {}

  /**
   * Get recent notifications
   */
  getRecentNotifications(count: number = 20): Observable<NotificationDto[]> {
    return this.api.get<NotificationDto[]>(this.endpoint, { count }).pipe(
      tap(notifications => {
        this.notificationsSubject.next(notifications);
        this.updateUnreadCount(notifications);
      })
    );
  }

  /**
   * Get notifications with filtering and pagination
   */
  getNotifications(filter?: NotificationFilterDto): Observable<PagedResult<NotificationDto>> {
    return this.api.get<PagedResult<NotificationDto>>(this.endpoint, filter);
  }

  /**
   * Get notification by ID
   */
  getNotification(id: number): Observable<NotificationDto> {
    return this.api.get<NotificationDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Get unread notification count
   */
  getUnreadCount(): Observable<{ count: number }> {
    return this.api.get<{ count: number }>(`${this.endpoint}/unread-count`).pipe(
      tap(response => this.unreadCountSubject.next(response.count))
    );
  }

  /**
   * Get notification summary
   */
  getSummary(): Observable<NotificationSummaryDto> {
    return this.api.get<NotificationSummaryDto>(`${this.endpoint}/summary`);
  }

  /**
   * Mark notification as read
   */
  markAsRead(id: number): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}/read`, {}).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.map(n => 
          n.notificationId === id ? { ...n, isRead: true, readAt: new Date() } : n
        );
        this.notificationsSubject.next(updated);
        this.updateUnreadCount(updated);
      })
    );
  }

  /**
   * Mark multiple notifications as read
   */
  markMultipleAsRead(dto: MarkNotificationsReadDto): Observable<any> {
    return this.api.post(`${this.endpoint}/mark-read`, dto).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.map(n => 
          dto.notificationIds.includes(n.notificationId) 
            ? { ...n, isRead: true, readAt: new Date() } 
            : n
        );
        this.notificationsSubject.next(updated);
        this.updateUnreadCount(updated);
      })
    );
  }

  /**
   * Mark all notifications as read
   */
  markAllAsRead(): Observable<any> {
    return this.api.post(`${this.endpoint}/mark-all-read`, {}).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.map(n => ({ ...n, isRead: true, readAt: new Date() }));
        this.notificationsSubject.next(updated);
        this.unreadCountSubject.next(0);
      })
    );
  }

  /**
   * Pin notification
   */
  pinNotification(id: number): Observable<any> {
    return this.api.post(`${this.endpoint}/${id}/pin`, {}).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.map(n => 
          n.notificationId === id ? { ...n, isPinned: true } : n
        );
        this.notificationsSubject.next(updated);
      })
    );
  }

  /**
   * Unpin notification
   */
  unpinNotification(id: number): Observable<any> {
    return this.api.post(`${this.endpoint}/${id}/unpin`, {}).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.map(n => 
          n.notificationId === id ? { ...n, isPinned: false } : n
        );
        this.notificationsSubject.next(updated);
      })
    );
  }

  /**
   * Delete notification
   */
  deleteNotification(id: number): Observable<any> {
    return this.api.delete(`${this.endpoint}/${id}`).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.filter(n => n.notificationId !== id);
        this.notificationsSubject.next(updated);
        this.updateUnreadCount(updated);
      })
    );
  }

  /**
   * Delete multiple notifications
   */
  deleteMultiple(notificationIds: number[]): Observable<any> {
    return this.api.post(`${this.endpoint}/delete-multiple`, { notificationIds }).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.filter(n => !notificationIds.includes(n.notificationId));
        this.notificationsSubject.next(updated);
        this.updateUnreadCount(updated);
      })
    );
  }

  /**
   * Get notification preferences
   */
  getPreferences(): Observable<NotificationPreferencesDto> {
    return this.api.get<NotificationPreferencesDto>(`${this.endpoint}/preferences`);
  }

  /**
   * Update notification preferences
   */
  updatePreferences(dto: UpdateNotificationPreferencesDto): Observable<any> {
    return this.api.put(`${this.endpoint}/preferences`, dto);
  }

  /**
   * Test notification
   */
  sendTestNotification(): Observable<any> {
    return this.api.post(`${this.endpoint}/test`, {});
  }

  /**
   * Update unread count from notification list
   */
  private updateUnreadCount(notifications: NotificationDto[]): void {
    const unreadCount = notifications.filter(n => !n.isRead).length;
    this.unreadCountSubject.next(unreadCount);
  }

  /**
   * Refresh notifications (call periodically or on user action)
   */
  refreshNotifications(): void {
    this.getRecentNotifications().subscribe();
    this.getUnreadCount().subscribe();
  }

  /**
   * Start polling for new notifications
   */
  startPolling(): void {
    if (this.polling$) {
      return; // Already polling
    }

    this.polling$ = interval(this.pollingInterval).pipe(
      switchMap(() => this.getUnreadCount())
    );

    this.polling$.subscribe();
  }

  /**
   * Stop polling for notifications
   */
  stopPolling(): void {
    this.polling$ = undefined;
  }

  /**
   * Set polling interval
   */
  setPollingInterval(milliseconds: number): void {
    this.pollingInterval = milliseconds;
    if (this.polling$) {
      this.stopPolling();
      this.startPolling();
    }
  }

  /**
   * Clear all notifications from local state
   */
  clearLocalNotifications(): void {
    this.notificationsSubject.next([]);
    this.unreadCountSubject.next(0);
  }
}