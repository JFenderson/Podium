// src/app/core/services/notification.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
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
  private apiUrl = `${environment.apiUrl}/Guardian/notifications`; // Target specific endpoint

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

  private processNotifications(notifications: NotificationDto[]): NotificationDto[] {
    return notifications
      .map(n => this.checkUrgency(n)) // 1. Check expiration logic
      .sort((a, b) => this.compareNotifications(a, b)); // 2. Sort logic
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
    const priorityWeight = {
      [NotificationPriority.Urgent]: 4,
      [NotificationPriority.High]: 3,
      [NotificationPriority.Normal]: 2,
      [NotificationPriority.Low]: 1
    };

    const weightDiff = priorityWeight[b.priority] - priorityWeight[a.priority];
    if (weightDiff !== 0) return weightDiff;

    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  }
}