import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth';
import { NotificationService } from './core/services/notification';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet></router-outlet>',
  styleUrls: ['./app.scss']
})
export class AppComponent implements OnInit {
  title = 'Podium - Band Recruitment Platform';

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    // Initialize authentication state
    this.authService.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) {
        // Load notifications for authenticated users
        this.notificationService.refreshNotifications();
        
        // Set up periodic notification refresh (every 30 seconds)
        setInterval(() => {
          this.notificationService.refreshNotifications();
        }, 30000);
      }
    });
  }
}