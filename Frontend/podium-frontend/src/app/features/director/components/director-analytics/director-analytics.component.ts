import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { DirectorService } from '../../services/director.service';
import { AnalyticsDashboardState } from '../../../../core/models/director.models';

@Component({
  selector: 'app-director-analytics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './director-analytics.component.html'
})
export class DirectorAnalyticsComponent implements OnInit {
  private directorService = inject(DirectorService);
  private route = inject(ActivatedRoute);

  isLoading = true;
  error = '';
  
  // Data State
  data: AnalyticsDashboardState | null = null;
  bandId: number = 0;

  ngOnInit(): void {
    // Assuming bandId is passed via route params or parent layout
    // For now, we'll try to get it from route or hardcode/fetch from user profile logic
    this.route.params.subscribe(params => {
      this.bandId = +params['bandId'] || 1; // Fallback or logic to get current band
      this.loadDashboard();
    });
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.directorService.getAnalyticsDashboard(this.bandId).subscribe({
      next: (dashboardData) => {
        this.data = dashboardData;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load analytics', err);
        this.error = 'Failed to load analytics data.';
        this.isLoading = false;
      }
    });
  }

  // Helper to calculate percentage width for CSS bars
  getPercent(value: number, total: number): string {
    if (!total || total === 0) return '0%';
    return Math.min(100, Math.round((value / total) * 100)) + '%';
  }
}