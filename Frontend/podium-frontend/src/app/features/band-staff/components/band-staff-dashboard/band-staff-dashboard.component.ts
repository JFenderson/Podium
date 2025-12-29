// ENHANCED band-staff-dashboard.component.ts
// Replace your existing file with this enhanced version

import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BandStaffService } from '../../services/band-staff.service';
import { StudentService } from '../../../student/services/student.service';
import { ScholarshipService } from '../../../scholarship/services/scholarship.service';
import { AuthService } from '../../../auth/services/auth.service';
import { BandStaffDto } from '../../../../core/models/band-staff.models';
import { Roles, Permissions } from '../../../../core/models/common.models';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

// Register Chart.js components
Chart.register(...registerables);

@Component({
  selector: 'app-band-staff-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './band-staff-dashboard.component.html',
  styleUrls: ['./band-staff-dashboard.component.scss']
})
export class BandStaffDashboardComponent implements OnInit, OnDestroy {
  private bandStaffService = inject(BandStaffService);
  private studentService = inject(StudentService);
  private scholarshipService = inject(ScholarshipService);
  private authService = inject(AuthService);

  profile: BandStaffDto | null = null;
  recentStudents: any[] = [];
  myOffers: any[] = [];
  recentActivity: any[] = []; // NEW
  
  isLoading = false;
  error: string | null = null;
  isBandStaff = false;

  // Stats
  totalStudentsViewed = 0;
  totalRatingsGiven = 0;
  totalOffersSent = 0;
  pendingOffers = 0;
  acceptedOffers = 0; // NEW
  acceptanceRate = 0; // NEW
  responseRate = 0; // NEW
  budgetAllocated = 50000; // NEW - Get from profile if available
  budgetUsed = 0; // NEW
  budgetRemaining = 0; // NEW

  // Permissions
  canViewStudents = false;
  canRateStudents = false;
  canSendOffers = false;
  canContactStudents = false;
  canManageEvents = false;

  // Chart
  private offersChart: Chart | null = null;

  ngOnInit(): void {
    this.isBandStaff = this.authService.hasRole(Roles.BandStaff) || this.authService.hasRole(Roles.Director);
    
    if (!this.isBandStaff) {
      this.error = 'Access denied. Band Staff role required.';
      return;
    }

    this.checkPermissions();
    this.loadProfile();
    this.loadDashboardData();
  }

  ngOnDestroy(): void {
    // Cleanup chart
    if (this.offersChart) {
      this.offersChart.destroy();
    }
  }

  checkPermissions(): void {
    this.canViewStudents = this.authService.hasPermission(Permissions.ViewStudents);
    this.canRateStudents = this.authService.hasPermission(Permissions.RateStudents);
    this.canSendOffers = this.authService.hasPermission(Permissions.SendOffers);
    this.canContactStudents = this.authService.hasPermission(Permissions.ContactStudents);
    this.canManageEvents = this.authService.hasPermission(Permissions.ManageEvents);
  }

  loadProfile(): void {
    this.bandStaffService.getMyProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
      },
      error: (error: any) => {
        console.error('Error loading profile:', error);
      }
    });
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.error = null;

    if (this.canViewStudents) {
      this.studentService.getStudents({ pageSize: 5, pageNumber: 1 }).subscribe({
        next: (result: any) => {
          this.recentStudents = result.items;
        },
        error: (error: any) => console.error('Error loading students:', error)
      });
    }

    if (this.canSendOffers) {
      this.scholarshipService.getOffers({ pageSize: 5, pageNumber: 1 }).subscribe({
        next: (result: any) => {
          this.myOffers = result.items;
          this.totalOffersSent = result.totalCount;
          this.pendingOffers = result.items.filter((o: any) => o.status === 'Sent').length;
          
          // NEW: Calculate acceptance metrics
          this.acceptedOffers = result.items.filter((o: any) => o.status === 'Accepted').length;
          this.acceptanceRate = this.totalOffersSent > 0 
            ? (this.acceptedOffers / this.totalOffersSent) * 100 
            : 0;
          
          // NEW: Calculate budget used
          this.budgetUsed = result.items
            .filter((o: any) => o.status === 'Accepted' || o.status === 'Sent')
            .reduce((sum: number, o: any) => sum + (o.amount || 0), 0);
          this.budgetRemaining = this.budgetAllocated - this.budgetUsed;
          
          this.isLoading = false;
          
          // NEW: Create chart after data loads
          setTimeout(() => this.createOffersChart(), 100);
        },
        error: (error: any) => {
          console.error('Error loading offers:', error);
          this.isLoading = false;
        }
      });
    } else {
      this.isLoading = false;
    }

    const currentUser = this.authService.currentUserValue;
    if (currentUser?.bandStaffId) {
      this.bandStaffService.getStaffStats(currentUser.bandStaffId).subscribe({
        next: (stats) => {
          this.totalStudentsViewed = stats.totalStudentsViewed || 0;
          this.totalRatingsGiven = stats.totalRatingsGiven || 0;
        },
        error: (error: any) => console.error('Error loading stats:', error)
      });
    }

    // NEW: Load activity feed
    this.loadRecentActivity();
  }

  // NEW: Load recent activity
  loadRecentActivity(): void {
    // Generate mock activity from offers and students
    // In production, you'd call a backend endpoint
    const activities: any[] = [];

    // Add offer activities
    this.myOffers.slice(0, 3).forEach(offer => {
      activities.push({
        icon: 'local_offer',
        description: `Sent $${offer.amount?.toLocaleString()} offer to ${offer.studentName}`,
        timestamp: offer.createdAt || new Date(),
        type: 'offer',
        color: 'text-blue-600'
      });
    });

    // Add student view activities
    this.recentStudents.slice(0, 2).forEach(student => {
      activities.push({
        icon: 'visibility',
        description: `Viewed ${student.firstName} ${student.lastName}'s profile`,
        timestamp: new Date(Date.now() - Math.random() * 86400000), // Random recent date
        type: 'view',
        color: 'text-purple-600'
      });
    });

    // Sort by timestamp
    this.recentActivity = activities
      .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
      .slice(0, 5);
  }

  // NEW: Create offers chart
  createOffersChart(): void {
    const canvas = document.getElementById('offersChart') as HTMLCanvasElement;
    if (!canvas) return;

    // Destroy existing chart
    if (this.offersChart) {
      this.offersChart.destroy();
    }

    // Generate mock data for last 6 months
    // In production, get this from backend
    const months = this.getLastMonths(6);
    const offersData = this.generateMockOfferData(6);

    const config: ChartConfiguration = {
      type: 'line',
      data: {
        labels: months,
        datasets: [
          {
            label: 'Offers Sent',
            data: offersData,
            borderColor: '#3b82f6',
            backgroundColor: 'rgba(59, 130, 246, 0.1)',
            tension: 0.4,
            fill: true,
            pointRadius: 4,
            pointHoverRadius: 6
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: false
          },
          tooltip: {
            mode: 'index',
            intersect: false
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              precision: 0
            }
          }
        }
      }
    };

    this.offersChart = new Chart(canvas, config);
  }

  // NEW: Helper to get last N months
  private getLastMonths(count: number): string[] {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const result: string[] = [];
    const now = new Date();
    
    for (let i = count - 1; i >= 0; i--) {
      const date = new Date(now.getFullYear(), now.getMonth() - i, 1);
      result.push(months[date.getMonth()]);
    }
    
    return result;
  }

  // NEW: Generate mock data (replace with real data from backend)
  private generateMockOfferData(count: number): number[] {
    // This generates realistic-looking data
    // In production, get actual data from backend
    const data: number[] = [];
    let current = Math.floor(Math.random() * 5) + 3;
    
    for (let i = 0; i < count; i++) {
      data.push(current);
      current = Math.max(0, current + Math.floor(Math.random() * 5) - 2);
    }
    
    return data;
  }

  // NEW: Calculate budget utilization percentage
  get budgetUtilization(): number {
    return this.budgetAllocated > 0 
      ? (this.budgetUsed / this.budgetAllocated) * 100 
      : 0;
  }

  // NEW: Format time ago
  getTimeAgo(date: Date): string {
    const now = new Date().getTime();
    const then = new Date(date).getTime();
    const diffMs = now - then;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return new Date(date).toLocaleDateString();
  }

  getOfferStatusColor(status: string): string {
    const colors: { [key: string]: string } = {
      'Draft': 'bg-gray-100 text-text-secondary',
      'Sent': 'bg-blue-100 text-blue-800',
      'Accepted': 'bg-green-100 text-green-800',
      'Declined': 'bg-red-100 text-red-800',
      'Expired': 'bg-yellow-100 text-yellow-800'
    };
    return colors[status] || 'bg-gray-100 text-text-secondary';
  }

  getRatingStars(rating: number): string {
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 >= 0.5;
    let stars = '★'.repeat(fullStars);
    if (hasHalfStar) stars += '⯨';
    stars += '☆'.repeat(5 - fullStars - (hasHalfStar ? 1 : 0));
    return stars;
  }
}