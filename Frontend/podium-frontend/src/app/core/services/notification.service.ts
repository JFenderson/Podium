import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, map, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { 
  NotificationDto, 
  NotificationFilterDto, 
  NotificationPriority
} from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Notifications`;

  // State
  private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$ = this.unreadCountSubject.asObservable();
  
  private notificationsSubject = new BehaviorSubject<NotificationDto[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  /**
   * 1. GET FILTERED ALERTS (Used by Notification List Page)
   * This was missing in the previous version!
   */
  getNotifications(filter: NotificationFilterDto): Observable<NotificationDto[]> {
    let params = new HttpParams();
    
    if (filter.type) params = params.set('type', filter.type);
    if (filter.priority) params = params.set('priority', filter.priority);
    if (filter.isRead !== undefined) params = params.set('isRead', filter.isRead.toString());
    if (filter.since) params = params.set('since', filter.since.toISOString());
    if (filter.pageNumber) params = params.set('page', filter.pageNumber);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);

    return this.http.get<any>(this.apiUrl, { params }).pipe(
      map(response => {
        // Handle potentially wrapped paginated response
        const items: NotificationDto[] = response.items || response || [];
        return this.processNotifications(items);
      })
    );
  }

  /**
   * 2. GET RECENT ALERTS (Used by Dashboard & Top Bar)
   */
  getRecentNotifications(count: number = 20): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>(this.apiUrl, { 
      params: new HttpParams().set('count', count.toString()) 
    }).pipe(
      map(notifications => this.processNotifications(notifications)),
      tap(notifications => {
        this.notificationsSubject.next(notifications);
        this.updateUnreadCount(notifications);
      })
    );
  }

  /**
   * Fetch unread count
   */
  getUnreadCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.apiUrl}/unread-count`).pipe(
      tap(response => this.unreadCountSubject.next(response.count))
    );
  }

  /**
   * Mark as read
   */
  markAsRead(id: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/read`, {}).pipe(
      tap(() => {
        this.updateLocalState(id, { isRead: true });
      })
    );
  }

  /**
   * Delete/Dismiss
   */
  deleteNotification(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        const current = this.notificationsSubject.value;
        const updated = current.filter(n => n.notificationId !== id);
        this.notificationsSubject.next(updated);
        this.updateUnreadCount(updated);
      })
    );
  }

  // --- Helpers ---

  private updateLocalState(id: number, changes: Partial<NotificationDto>) {
    const current = this.notificationsSubject.value;
    const updated = current.map(n => n.notificationId === id ? { ...n, ...changes } : n);
    this.notificationsSubject.next(updated);
    this.updateUnreadCount(updated);
  }

  private updateUnreadCount(notifications: NotificationDto[]): void {
    const count = notifications.filter(n => !n.isRead).length;
    this.unreadCountSubject.next(count);
  }

  private processNotifications(notifications: NotificationDto[]): NotificationDto[] {
    return notifications
      .map(n => this.checkUrgency(n))
      .sort((a, b) => this.compareNotifications(a, b));
  }

  private checkUrgency(notification: NotificationDto): NotificationDto {
    if (notification.expiresAt && !notification.isRead) {
      const hoursUntilExpiration = (new Date(notification.expiresAt).getTime() - new Date().getTime()) / (1000 * 60 * 60);
      
      if (hoursUntilExpiration <= 24 && hoursUntilExpiration > 0) {
        return { ...notification, priority: NotificationPriority.Urgent };
      } else if (hoursUntilExpiration <= 72) {
        if (notification.priority === NotificationPriority.Low || notification.priority === NotificationPriority.Medium) {
          return { ...notification, priority: NotificationPriority.High };
        }
      }
    }
    return notification;
  }

  private compareNotifications(a: NotificationDto, b: NotificationDto): number {
    const priorityWeight: Record<string, number> = {
      [NotificationPriority.Urgent]: 4,
      [NotificationPriority.High]: 3,
      [NotificationPriority.Medium]: 2,
      [NotificationPriority.Low]: 1
    };

    const pA = priorityWeight[a.priority] || 0;
    const pB = priorityWeight[b.priority] || 0;

    const weightDiff = pB - pA;
    if (weightDiff !== 0) return weightDiff;

    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  }
}