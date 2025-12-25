import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StudentService } from '../../services/student.service';
import { StudentDashboardDto } from '../../../../core/models/student.models';
import { Clipboard } from '@angular/cdk/clipboard'; // Optional: for 'Copy Code' button
import { NotificationCardComponent } from '../../../../shared/components/notifications/notification-card.component';
import { NotificationService } from '../../../../core/services/notification.service';
import { NotificationDto } from '../../../../core/models/notification.models';


@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, NotificationCardComponent], // Add MatIconModule if using Angular Material
  templateUrl: './student-dashboard.component.html',
  styleUrls: ['./student-dashboard.component.scss']
})
export class StudentDashboardComponent implements OnInit {
  private studentService = inject(StudentService);
  private clipboard = inject(Clipboard);
  public notificationService = inject(NotificationService);

  dashboard: StudentDashboardDto | null = null;
  isLoading = true;
  copySuccess = false;

  ngOnInit(): void {
    this.loadDashboard();
    this.notificationService.getRecentNotifications().subscribe();
    this.notificationService.getUnreadCount().subscribe();
  }

  loadDashboard(): void {
    this.studentService.getDashboard().subscribe({
      next: (data: StudentDashboardDto) => {
        this.dashboard = data;
        this.isLoading = false;
      },
      error: (err: any) => {
        console.error('Failed to load dashboard', err);
        this.isLoading = false;
      }
    });
  }

  copyInviteCode(code: string): void {
    const success = this.clipboard.copy(code);
    if (success) {
      this.copySuccess = true;
      setTimeout(() => this.copySuccess = false, 2000);
    }
  }

  onMarkAsRead(notification: NotificationDto): void {
    this.notificationService.markAsRead(notification.notificationId).subscribe({
      next: () => console.log('Marked as read'),
      error: (err) => console.error('Error marking as read', err)
    });
  }

  onDismiss(notification: NotificationDto): void {
    this.notificationService.deleteNotification(notification.notificationId).subscribe({
      next: () => console.log('Dismissed notification'),
      error: (err) => console.error('Error dismissing notification', err)
    });
  }
}