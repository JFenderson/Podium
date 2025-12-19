import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { GuardianService } from '../../services/guardian.service';
import { AuthService } from '../../../auth/services/auth.service';
import {
  GuardianDashboardDto,
  GuardianPendingApprovalDto,
  GuardianApprovalDto,
  GuardianLinkedStudentDto
} from '../../../../core/models/guardian.models';
import { Roles } from '../../../../core/models/common.models';

@Component({
  selector: 'app-guardian-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './guardian-dashboard.component.html'
})
export class GuardianDashboardComponent implements OnInit {
  private guardianService = inject(GuardianService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  dashboard: GuardianDashboardDto | null = null;
  isLoading = false;
  isProcessingApproval = false;
  error: string | null = null;
  successMessage: string | null = null;
  isGuardian = false;

  // Modal states
  showApprovalModal = false;
  selectedApproval: GuardianPendingApprovalDto | null = null;

  approvalForm: FormGroup = this.fb.group({
    approved: [true],
    notes: ['', Validators.maxLength(500)]
  });

  ngOnInit(): void {
    this.isGuardian = this.authService.hasRole(Roles.Guardian);
    
    if (!this.isGuardian) {
      this.error = 'Access denied. Guardian role required.';
      return;
    }

    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.error = null;

    this.guardianService.getDashboard().subscribe({
      next: (dashboard: any) => {
        this.dashboard = dashboard;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.error = 'Failed to load dashboard. Please try again.';
        this.isLoading = false;
        console.error('Error loading dashboard:', error);
      }
    });
  }

  openApprovalModal(approval: GuardianPendingApprovalDto): void {
    this.selectedApproval = approval;
    this.approvalForm.reset({ approved: true });
    this.showApprovalModal = true;
  }

  closeApprovalModal(): void {
    this.showApprovalModal = false;
    this.selectedApproval = null;
    this.approvalForm.reset();
  }

  submitApproval(): void {
    if (!this.selectedApproval) return;

    this.isProcessingApproval = true;
    this.error = null;

    const dto: GuardianApprovalDto = {
      offerId: this.selectedApproval.offerId,
      approved: this.approvalForm.value.approved,
      notes: this.approvalForm.value.notes
    };

    this.guardianService.approveOffer(this.selectedApproval.offerId, dto).subscribe({
      next: () => {
        this.isProcessingApproval = false;
        this.closeApprovalModal();
        this.successMessage = dto.approved 
          ? 'Offer approved successfully!' 
          : 'Offer declined successfully.';
        this.loadDashboard();
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (error: any) => {
        this.error = 'Failed to process approval. Please try again.';
        this.isProcessingApproval = false;
        console.error('Error processing approval:', error);
      }
    });
  }

  getActivityIcon(type: string): string {
    const icons: { [key: string]: string } = {
      'OfferReceived': 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
      'OfferAccepted': 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
      'OfferDeclined': 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z',
      'InterestShown': 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z',
      'VideoUploaded': 'M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z',
      'default': 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
    };
    return icons[type] || icons['default'];
  }

  getActivityColor(type: string): string {
    const colors: { [key: string]: string } = {
      'OfferReceived': 'bg-blue-100 text-blue-600',
      'OfferAccepted': 'bg-green-100 text-green-600',
      'OfferDeclined': 'bg-red-100 text-red-600',
      'ApprovalGranted': 'bg-green-100 text-green-600',
      'ApprovalDenied': 'bg-red-100 text-red-600',
      'InterestShown': 'bg-purple-100 text-purple-600',
      'VideoUploaded': 'bg-indigo-100 text-indigo-600',
      'default': 'bg-gray-100 text-gray-600'
    };
    return colors[type] || colors['default'];
  }

  getUrgencyClass(expiresAt?: Date): string {
    if (!expiresAt) return '';
    
    const now = new Date();
    const expires = new Date(expiresAt);
    const daysUntilExpiry = Math.ceil((expires.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    
    if (daysUntilExpiry <= 3) {
      return 'border-red-500 bg-red-50';
    } else if (daysUntilExpiry <= 7) {
      return 'border-yellow-500 bg-yellow-50';
    }
    return 'border-gray-200';
  }
}