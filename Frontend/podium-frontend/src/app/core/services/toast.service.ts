import { Injectable, signal } from '@angular/core';
import { NotificationPriority } from '../models/notification.models';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
  priority?: NotificationPriority;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  toasts = signal<Toast[]>([]);
  private nextId = 0;

show(message: string, type: Toast['type'] = 'info', priority: NotificationPriority = NotificationPriority.Low) {
    const id = this.nextId++;
    const duration = this.getDurationByPriority(priority);
    
    this.toasts.update(toasts => [...toasts, { id, message, type, priority }]);
    
    if (duration > 0) {
      setTimeout(() => this.remove(id), duration);
    }
  }

  private getDurationByPriority(priority: NotificationPriority): number {
    switch (priority) {
      case NotificationPriority.Urgent: return 10000; // 10s
      case NotificationPriority.High: return 7000;    // 7s
      case NotificationPriority.Normal: return 5000;  // 5s
      default: return 3000;                           // 3s (Low)
    }
  }

  remove(id: number) {
    this.toasts.update(toasts => toasts.filter(t => t.id !== id));
  }

  success(message: string) { this.show(message, 'success'); }
  error(message: string) { this.show(message, 'error'); }
  info(message: string) { this.show(message, 'info'); }
  warning(message: string) { this.show(message, 'warning'); }
}