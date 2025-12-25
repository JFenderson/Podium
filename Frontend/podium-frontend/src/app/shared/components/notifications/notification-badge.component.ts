// notification-badge.component.ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationPriority } from '../../../core/models/notification.models';

@Component({
  selector: 'app-notification-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span [ngClass]="getBadgeClasses()" 
          class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border">
      <span [ngClass]="getDotClasses()" class="w-1.5 h-1.5 rounded-full mr-1.5"></span>
      {{ priority }}
    </span>
  `
})
export class NotificationBadgeComponent {
  @Input({ required: true }) priority!: NotificationPriority;

  getBadgeClasses(): string {
    switch (this.priority) {
      case NotificationPriority.Urgent: return 'bg-red-100 text-red-800 border-red-200';
      case NotificationPriority.High:   return 'bg-orange-100 text-orange-800 border-orange-200';
      case NotificationPriority.Medium: return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case NotificationPriority.Low:    return 'bg-blue-100 text-blue-800 border-blue-200';
      default:                          return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  }

  getDotClasses(): string {
    switch (this.priority) {
      case NotificationPriority.Urgent: return 'bg-red-600';
      case NotificationPriority.High:   return 'bg-orange-600';
      case NotificationPriority.Medium: return 'bg-yellow-600';
      case NotificationPriority.Low:    return 'bg-blue-600';
      default:                          return 'bg-gray-600';
    }
  }
}