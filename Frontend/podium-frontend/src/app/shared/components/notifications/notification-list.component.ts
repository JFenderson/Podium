import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificationService } from '../../../core/services/notification.service';
// CHECK THIS PATH: ensure it points to where you saved the file in step 2
import { NotificationCardComponent } from './notification-card.component';
import { NotificationDto, NotificationPriority } from '../../../core/models/notification.models';

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [CommonModule, FormsModule, NotificationCardComponent],
  template: `
    <div class="space-y-6">
      <div class="flex gap-4 items-center bg-white p-4 rounded-lg shadow-sm border border-gray-100">
        <select [(ngModel)]="selectedPriority" (change)="loadNotifications()" 
                class="block w-40 rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 text-sm">
          <option [ngValue]="null">All Priorities</option>
          <option [value]="Priority.Urgent">Urgent</option>
          <option [value]="Priority.High">High</option>
          <option [value]="Priority.Medium">Medium</option>
          <option [value]="Priority.Low">Low</option>
        </select>

        <label class="flex items-center space-x-2 text-sm text-gray-700">
          <input type="checkbox" [(ngModel)]="showUnreadOnly" (change)="loadNotifications()"
                 class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500">
          <span>Unread Only</span>
        </label>
      </div>

      <div class="space-y-2">
        <app-notification-card 
          *ngFor="let notification of notifications()" 
          [notification]="notification"
          (markRead)="onMarkRead($event)"
          (dismiss)="onDismiss($event)">
        </app-notification-card>

        <div *ngIf="notifications().length === 0" class="text-center py-10 text-gray-500">
          No notifications found.
        </div>
      </div>
    </div>
  `
})
export class NotificationListComponent implements OnInit {
  private service = inject(NotificationService);
  
  Priority = NotificationPriority;
  notifications = signal<NotificationDto[]>([]);
  
  selectedPriority: NotificationPriority | null = null;
  showUnreadOnly = false;

  ngOnInit() {
    this.loadNotifications();
  }

  loadNotifications() {
    // Now this will work because we added getNotifications back to the service
    this.service.getNotifications({
      priority: this.selectedPriority || undefined,
      isRead: this.showUnreadOnly ? false : undefined,
      pageNumber: 1,
      pageSize: 50
    }).subscribe(data => {
      this.notifications.set(data);
    });
  }

  onMarkRead(notification: NotificationDto) {
    this.service.markAsRead(notification.notificationId).subscribe();
  }

  // Added missing handler
  onDismiss(notification: NotificationDto) {
    this.service.deleteNotification(notification.notificationId).subscribe(() => {
        this.loadNotifications(); // Reload list after delete
    });
  }
}