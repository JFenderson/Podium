import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DirectorService } from '../../services/director';
import { BandStaffService } from '../../../band-staff/services/band-staff';
import { AuthService } from '../../../../features/auth/services/auth';
import { 
  DirectorDashboardDto, 
  BandStatisticsDto,
  DirectorActivityDto 
} from '../../../../core/models/director';
import { Roles } from '../../../../core/models/common';

@Component({
  selector: 'app-director-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './director-dashboard.html'
})
export class DirectorDashboardComponent implements OnInit {
  private directorService = inject(DirectorService);
  private bandStaffService = inject(BandStaffService);
  private authService = inject(AuthService);

  dashboard: DirectorDashboardDto | null = null;
  statistics: BandStatisticsDto | null = null;
  isLoading = false;
  isStatsLoading = false;
  error: string | null = null;
  isDirector = false;
  bandId: number | null = null;

  // Chart data
  offerStatusData: any[] = [];
  instrumentData: any[] = [];

  ngOnInit(): void {
    this.isDirector = this.authService.hasRole(Roles.Director);
    
    if (!this.isDirector) {
      this.error = 'Access denied. Director role required.';
      return;
    }

    const currentUser = this.authService.currentUserValue;
    this.bandId = currentUser?.bandId || null;

    this.loadDashboard();
    if (this.bandId) {
      this.loadStatistics(this.bandId);
    }
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.error = null;

    this.directorService.getDashboard().subscribe({
      next: (dashboard) => {
        this.dashboard = dashboard;
        this.prepareChartData();
        this.isLoading = false;
      },
      error: (error) => {
        this.error = 'Failed to load dashboard. Please try again.';
        this.isLoading = false;
        console.error('Error loading dashboard:', error);
      }
    });
  }

  loadStatistics(bandId: number): void {
    this.isStatsLoading = true;

    this.directorService.getBandStatistics(bandId).subscribe({
      next: (statistics) => {
        this.statistics = statistics;
        this.isStatsLoading = false;
      },
      error: (error) => {
        console.error('Error loading statistics:', error);
        this.isStatsLoading = false;
      }
    });
  }

  prepareChartData(): void {
    if (!this.dashboard) return;

    // Prepare offer status data
    this.offerStatusData = this.dashboard.offersByStatus || [];

    // Prepare instrument distribution data
    this.instrumentData = this.dashboard.studentsByInstrument || [];
  }

  getActivityIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'OfferCreated': 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
      'OfferSent': 'M12 19l9 2-9-18-9 18 9-2zm0 0v-8',
      'OfferAccepted': 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
      'OfferDeclined': 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z',
      'StudentRated': 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z',
      'StaffAdded': 'M18 9v3m0 0v3m0-3h3m-3 0h-3m-2-5a4 4 0 11-8 0 4 4 0 018 0zM3 20a6 6 0 0112 0v1H3v-1z',
      'default': 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
    };
    return icons[type] || icons['default'];
  }

  getActivityColor(type: string): string {
    const colors: { [key: string]: string } = {
      'OfferCreated': 'bg-blue-100 text-blue-600',
      'OfferSent': 'bg-indigo-100 text-indigo-600',
      'OfferAccepted': 'bg-green-100 text-green-600',
      'OfferDeclined': 'bg-red-100 text-red-600',
      'StudentRated': 'bg-yellow-100 text-yellow-600',
      'StaffAdded': 'bg-purple-100 text-purple-600',
      'default': 'bg-gray-100 text-gray-600'
    };
    return colors[type] || colors['default'];
  }

  getOfferStatusColor(status: string): string {
    const colors: { [key: string]: string } = {
      'Draft': 'bg-gray-500',
      'Sent': 'bg-blue-500',
      'Accepted': 'bg-green-500',
      'Declined': 'bg-red-500',
      'Expired': 'bg-yellow-500',
      'Withdrawn': 'bg-orange-500'
    };
    return colors[status] || 'bg-gray-500';
  }

  refresh(): void {
    this.loadDashboard();
    if (this.bandId) {
      this.loadStatistics(this.bandId);
    }
  }

  exportData(): void {
    if (!this.bandId) return;

    this.directorService.exportDashboardData(this.bandId, 'excel').subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `dashboard-report-${new Date().toISOString().split('T')[0]}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        this.error = 'Failed to export data. Please try again.';
        console.error('Error exporting data:', error);
      }
    });
  }
}