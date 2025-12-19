import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DirectorService } from '../../services/director.service';
import { BandStaffService } from '../../../band-staff/services/band-staff.service';
import { AuthService } from '../../../auth/services/auth.service';
import { 
  DirectorDashboardDto, 
  BandStatisticsDto,
  DirectorActivityDto 
} from '../../../../core/models/director.models';
import { Roles } from '../../../../core/models/common.models';


@Component({
  selector: 'app-director-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatDividerModule
  ],
  templateUrl: './director-dashboard.component.html',
  styleUrls: ['./director-dashboard.component.scss']
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
      next: (dashboard: DirectorDashboardDto) => {
        this.dashboard = dashboard;
        this.prepareChartData();
        this.isLoading = false;
      },
      error: (error: any) => {
        this.error = 'Failed to load dashboard. Please try again.';
        this.isLoading = false;
        console.error('Error loading dashboard:', error);
      }
    });
  }

  loadStatistics(bandId: number): void {
    this.isStatsLoading = true;

    this.directorService.getBandStatistics(bandId).subscribe({
      next: (statistics: BandStatisticsDto) => {
        this.statistics = statistics;
        this.isStatsLoading = false;
      },
      error: (error: any) => {
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
      'OfferCreated': 'mail',
      'OfferSent': 'send',
      'OfferAccepted': 'check_circle',
      'OfferDeclined': 'cancel',
      'StudentRated': 'star',
      'StaffAdded': 'person_add',
      'default': 'info'
    };
    return icons[type] || icons['default'];
  }

  getActivityColor(type: string): 'primary' | 'accent' | 'warn' | undefined {
    const colors: { [key: string]: 'primary' | 'accent' | 'warn' | undefined } = {
      'OfferCreated': 'primary',
      'OfferSent': 'primary',
      'OfferAccepted': 'accent',
      'OfferDeclined': 'warn',
      'StudentRated': 'accent',
      'StaffAdded': 'primary',
      'default': undefined
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
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `dashboard-report-${new Date().toISOString().split('T')[0]}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (error: any) => {
        this.error = 'Failed to export data. Please try again.';
        console.error('Error exporting data:', error);
      }
    });
  }
}

