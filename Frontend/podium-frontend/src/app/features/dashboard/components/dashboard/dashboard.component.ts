import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../../features/auth/services/auth.service';
import { Roles } from '../../../../core/models/common.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  template: '' // No HTML needed, we redirect immediately
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  ngOnInit() {
    const user = this.authService.currentUserValue;
    
    if (!user || !user.roles) {
      this.router.navigate(['/login']);
      return;
    }

    if (user.roles.includes(Roles.Guardian)) {
      this.router.navigate(['/guardian/dashboard']);
    } else if (user.roles.includes(Roles.Director) || user.roles.includes(Roles.BandStaff)) {
      this.router.navigate(['/director/dashboard']);
    } else if (user.roles.includes(Roles.Student)) {
      this.router.navigate(['/students/profile']); // Or /student/dashboard if you have one
    } else {
      // Fallback for Admin or unknown roles
      this.router.navigate(['/profile']); 
    }
  }
}