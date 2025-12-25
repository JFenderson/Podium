// src/app/core/services/notification.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { 
  NotificationDto, 
  NotificationFilterDto, 
  NotificationPriority,
  NotificationType
} from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
private apiUrl = `${environment.apiUrl}/Notifications`;

private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$ = this.unreadCountSubject.asObservable();
  
  private notificationsSubject = new BehaviorSubject<NotificationDto[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  getNotifications(filter: NotificationFilterDto): Observable<NotificationDto[]> {
    let params = new HttpParams();
    
    if (filter.type) params = params.set('type', filter.type);
    if (filter.priority) params = params.set('priority', filter.priority); // Integrated filtering
    if (filter.isRead !== undefined) params = params.set('isRead', filter.isRead.toString());
    if (filter.since) params = params.set('since', filter.since.toISOString());
    if (filter.pageNumber) params = params.set('page', filter.pageNumber);
    if (filter.pageSize) params = params.set('pageSize', filter.pageSize);

    return this.http.get<any>(this.apiUrl, { params }).pipe(
      map(response => {
        // Handle paginated response if wrapped, otherwise assume array
        const items: NotificationDto[] = response.items || response || [];
        return this.processNotifications(items);
      })
    );
  }

getRecentNotifications(count: number = 20): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>(this.apiUrl, { params: new HttpParams().set('count', count.toString()) }).pipe(
      tap(notifications => {
        // --- 2. Fix Sorting Error ---
        // Use the helper method instead of (b - a) to support String Enums safely
        const sorted = notifications.sort((a, b) => this.compareNotifications(a, b));
        
        // --- 3. Fix Missing Property Access ---
        this.notificationsSubject.next(sorted);
        this.updateUnreadCount(sorted);
      })
    );
  }


  private processNotifications(notifications: NotificationDto[]): NotificationDto[] {
    return notifications
      .map(n => this.checkUrgency(n)) // 1. Check expiration logic
      .sort((a, b) => this.compareNotifications(a, b)); // 2. Sort logic
  }

  private updateUnreadCount(notifications: NotificationDto[]): void {
    const unreadCount = notifications.filter(n => !n.isRead).length;
    this.unreadCountSubject.next(unreadCount);
  }

  // Upgrade priority if expiration is imminent (< 24 hours)
  private checkUrgency(notification: NotificationDto): NotificationDto {
    if (notification.expiresAt && !notification.isRead) {
      const hoursUntilExpiration = (new Date(notification.expiresAt).getTime() - new Date().getTime()) / (1000 * 60 * 60);
      
      if (hoursUntilExpiration <= 24 && hoursUntilExpiration > 0) {
        return { ...notification, priority: NotificationPriority.Urgent };
      } else if (hoursUntilExpiration <= 72) {
         // Upgrade Low/Medium to High if expiring soon
        if (notification.priority === NotificationPriority.Low || notification.priority === NotificationPriority.Normal) {
          return { ...notification, priority: NotificationPriority.High };
        }
      }
    }
    return notification;
  }

  // Sort by Priority (Urgent -> Low) then Date (Newest -> Oldest)
private compareNotifications(a: NotificationDto, b: NotificationDto): number {
    // Defines weight explicitly to handle both String and Number Enums
    const priorityWeight: Record<string, number> = {
      [NotificationPriority.Urgent]: 4,
      [NotificationPriority.High]: 3,
      [NotificationPriority.Normal]: 2, // Changed from Normal to Medium to match typical Enum
      [NotificationPriority.Low]: 1
    };

    // If undefined (e.g. enum mismatch), treat as lowest priority
    const pA = priorityWeight[a.priority] || 0;
    const pB = priorityWeight[b.priority] || 0;

    const weightDiff = pB - pA;
    if (weightDiff !== 0) return weightDiff;

    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  }
}