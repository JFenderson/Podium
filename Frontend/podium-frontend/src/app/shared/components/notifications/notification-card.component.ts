import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { NotificationDto, NotificationPriority } from '../../../core/models/notification.models';
import { NotificationBadgeComponent } from './notification-badge.component';

@Component({
  selector: 'app-notification-card',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, NotificationBadgeComponent],
  template: `
    <div [ngClass]="containerClasses" 
         class="relative w-full p-4 bg-white rounded-lg shadow-sm border border-gray-100 hover:shadow-md transition-all duration-200 mb-3">
      
      <div [ngClass]="borderClasses" class="absolute left-0 top-0 bottom-0 w-1 rounded-l-lg"></div>

      <div class="flex justify-between items-start pl-3">
        <div class="flex-1">
          <div class="flex items-center gap-2 mb-1">
            <h4 class="text-sm font-semibold text-gray-900">{{ notification.title }}</h4>
            <app-notification-badge [priority]="notification.priority"></app-notification-badge>
          </div>
          <p class="text-sm text-gray-600">{{ notification.message }}</p>
          <div class="flex justify-between items-center mt-2">
            <span class="text-xs text-gray-400">{{ notification.createdAt | date:'short' }}</span>
            
            <div class="flex gap-2">
               <button *ngIf="!notification.isRead" 
                       (click)="markRead.emit(notification)"
                       class="text-xs text-indigo-600 hover:text-indigo-800">
                 Mark Read
               </button>
               <button (click)="dismiss.emit(notification)"
                       class="text-xs text-red-500 hover:text-red-700">
                 Dismiss
               </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class NotificationCardComponent {
  @Input({ required: true }) notification!: NotificationDto;
  
  // These Output types MUST match what onMarkRead and onDismiss expect in the parent
  @Output() markRead = new EventEmitter<NotificationDto>();
  @Output() dismiss = new EventEmitter<NotificationDto>();

  get borderClasses(): string {
    switch (this.notification.priority) {
      case NotificationPriority.Urgent: return 'bg-red-500';
      case NotificationPriority.High:   return 'bg-orange-500';
      case NotificationPriority.Medium: return 'bg-yellow-500';
      default:                          return 'bg-blue-500';
    }
  }

  get containerClasses(): string {
    return this.notification.isRead ? 'bg-gray-50 opacity-75' : 'bg-white';
  }
}