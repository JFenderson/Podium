import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { BandService } from '../../services/band.service';
import { StudentService } from '../../../student/services/student.service';
import { AuthService } from '../../../auth/services/auth.service';
import { BandDetailDto } from '../../../../core/models/band.models';
import { Roles } from '../../../../core/models/common.models';

@Component({
  selector: 'app-band-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './band-detail.component.html'
})
export class BandDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private bandService = inject(BandService);
  private studentService = inject(StudentService);
  private authService = inject(AuthService);

  band: BandDetailDto | null = null;
  isLoading = false;
  error: string | null = null;
  isStudent = false;
  isShowingInterest = false;
  interestShown = false;

  ngOnInit(): void {
    this.isStudent = this.authService.hasRole(Roles.Student);
    this.loadBandDetails();
  }

  loadBandDetails(): void {
    const bandId = Number(this.route.snapshot.paramMap.get('id'));
    
    if (!bandId) {
      this.router.navigate(['/bands']);
      return;
    }

    this.isLoading = true;
    this.error = null;

    this.bandService.getBand(bandId).subscribe({
      next: (band: any) => {
        this.band = band;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.error = 'Failed to load band details. Please try again.';
        this.isLoading = false;
        console.error('Error loading band details:', error);
      }
    });
  }

  showInterest(): void {
    if (!this.band || !this.isStudent) {
      return;
    }

    const currentUser = this.authService.currentUserValue;
    if (!currentUser?.studentId) {
      this.error = 'Student profile not found';
      return;
    }

    this.isShowingInterest = true;
    this.error = null;

    this.studentService.showInterest({
      studentId: currentUser.studentId,
      bandId: this.band.bandId,
      interestedAt: new Date()
    }).subscribe({
      next: () => {
        this.interestShown = true;
        this.isShowingInterest = false;
        // Show success message
        this.showSuccessToast('Interest submitted successfully!');
      },
      error: (error: any) => {
        this.error = 'Failed to submit interest. Please try again.';
        this.isShowingInterest = false;
        console.error('Error showing interest:', error);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/bands']);
  }

  private showSuccessToast(message: string): void {
    // In a real app, use a toast service
    alert(message);
  }
}