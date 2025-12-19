import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BandStaffService } from '../../services/band-staff.service';
import { StudentService } from '../../../student/services/student.service';
import { ScholarshipService } from '../../../scholarship/services/scholarship.service';
import { AuthService } from '../../../auth/services/auth.service';
import { BandStaffDto } from '../../../../core/models/band-staff.models';
import { Roles, Permissions } from '../../../../core/models/common.models';

@Component({
  selector: 'app-band-staff-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './band-staff-dashboard.component.html',
  styleUrls: ['./band-staff-dashboard.component.scss']
})
export class BandStaffDashboardComponent implements OnInit {
  private bandStaffService = inject(BandStaffService);
  private studentService = inject(StudentService);
  private scholarshipService = inject(ScholarshipService);
  private authService = inject(AuthService);

  profile: BandStaffDto | null = null;
  recentStudents: any[] = [];
  myOffers: any[] = [];
  
  isLoading = false;
  error: string | null = null;
  isBandStaff = false;

  // Stats
  totalStudentsViewed = 0;
  totalRatingsGiven = 0;
  totalOffersSent = 0;
  pendingOffers = 0;

  // Permissions
  canViewStudents = false;
  canRateStudents = false;
  canSendOffers = false;
  canContactStudents = false;
  canManageEvents = false;

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
          this.isLoading = false;
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
    if (hasHalfStar) stars += '⯨'; // You might want to replace this with half-star icon logic in HTML if using Material Icons
    stars += '☆'.repeat(5 - fullStars - (hasHalfStar ? 1 : 0));
    return stars;
  }
}