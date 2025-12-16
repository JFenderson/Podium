import { Component, inject, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { GuardianService } from '../../../core/services/guardian.service';
import { GuardianDashboardDto } from '../../../core/models/guardian.models';

@Component({
  selector: 'app-guardian-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './guardian-dashboard.component.html'
})
export class GuardianDashboardComponent implements OnInit {
  private guardianService = inject(GuardianService);
  dashboard: GuardianDashboardDto | null = null;
  isLoading = true;

  ngOnInit() {
    this.guardianService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard = data;
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }
}