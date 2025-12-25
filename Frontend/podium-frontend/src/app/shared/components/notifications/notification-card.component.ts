// notification-card.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, AlertCircle, AlertTriangle, Info, Bell, X, Check } from 'lucide-angular';
import { NotificationDto, NotificationPriority } from '../../../core/models/notification.models';
import { NotificationBadgeComponent } from './notification-badge.component';

@Component({
  selector: 'app-notification-card',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, NotificationBadgeComponent],
  template: `
    <div [ngClass]="getCardClasses()" 
         class="relative w-full p-4 rounded-lg border-l-4 shadow-sm bg-white hover:shadow-md transition-shadow duration-200 mb-3">
      
      <div class="flex justify-between items-start mb-2">
        <div class="flex items-center gap-2">
          <lucide-icon [name]="getIconName()" [class]="getIconColor()" class="w-5 h-5"></lucide-icon>
          <h4 class="font-semibold text-gray-900">{{ notification.title }}</h4>
        </div>
        <app-notification-badge [priority]="notification.priority"></app-notification-badge>
      </div>

      <p class="text-gray-600 text-sm mb-3 pl-7">{{ notification.message }}</p>

      <div class="flex justify-between items-center pl-7">
        <span class="text-xs text-gray-400">{{ notification.createdAt | date:'medium' }}</span>
        
        <div class="flex gap-2">
           <button *ngIf="notification.actionUrl" 
                   (click)="action.emit(notification)"
                   class="text-sm text-indigo-600 hover:text-indigo-800 font-medium">
             View Details
           </button>
           <button (click)="markRead.emit(notification)" 
                   class="text-gray-400 hover:text-green-600" 
                   title="Mark as Read">
             <lucide-icon name="check" class="w-4 h-4"></lucide-icon>
           </button>
        </div>
      </div>
    </div>
  `
})
export class NotificationCardComponent {
  @Input({ required: true }) notification!: NotificationDto;
  @Output() markRead = new EventEmitter<NotificationDto>();
  @Output() action = new EventEmitter<NotificationDto>();

  getCardClasses(): string {
    // Priority borders
    switch (this.notification.priority) {
      case NotificationPriority.Urgent: return 'border-l-red-500';
      case NotificationPriority.High:   return 'border-l-orange-500';
      case NotificationPriority.Normal: return 'border-l-yellow-500';
      case NotificationPriority.Low:    return 'border-l-blue-500';
      default:                          return 'border-l-gray-300';
    }
  }

  getIconName(): string {
    switch (this.notification.priority) {
      case NotificationPriority.Urgent: return 'alert-circle';
      case NotificationPriority.High:   return 'alert-triangle';
      default:                          return 'bell';
    }
  }

  getIconColor(): string {
    switch (this.notification.priority) {
      case NotificationPriority.Urgent: return 'text-red-500';
      case NotificationPriority.High:   return 'text-orange-500';
      case NotificationPriority.Normal: return 'text-yellow-500';
      case NotificationPriority.Low:    return 'text-blue-500';
      default:                          return 'text-gray-400';
    }
  }
}