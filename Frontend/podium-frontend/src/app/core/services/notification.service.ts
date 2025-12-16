import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApiService } from '../../core/services/api.service';
import { NotificationDto } from '../../core/models/common.models';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly endpoint = 'Notifications';
  
  private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$ = this.unreadCountSubject.asObservable();
  
  private notificationsSubject = new BehaviorSubject<NotificationDto[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

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
   * Get unread notification count
   */
  getUnreadCount(): Observable<{ count: number }> {
    return this.api.get<{ count: number }>(`${this.endpoint}/unread-count`).pipe(
      tap(response => this.unreadCountSubject.next(response.count))
    );
  }

  /**
   * Mark notification as read
   */
  markAsRead(id: number): Observable<any> {
    return this.api.put(`${this.endpoint}/${id}/read`, {}).pipe(
      tap(() => {
        // Update local state
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
}