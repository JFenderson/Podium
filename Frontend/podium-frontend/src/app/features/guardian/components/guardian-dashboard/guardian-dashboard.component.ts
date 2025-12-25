import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { GuardianService } from '../../services/guardian.service';
import { AuthService } from '../../../../features/auth/services/auth.service';
import { GuardianDashboardDto, GuardianPendingApprovalDto } from '../../../../core/models/guardian.models';
import { Roles } from '../../../../core/models/common.models';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';

@Component({
  selector: 'app-guardian-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule,
    SkeletonLoaderComponent
  ],
  templateUrl: './guardian-dashboard.component.html'
})
export class GuardianDashboardComponent implements OnInit {
  private guardianService = inject(GuardianService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  // Define dashboard as a Signal to fix template type errors
  dashboard = signal<GuardianDashboardDto | null>(null);
  
  isLoading = false;
  isProcessingApproval = false;
  error: string | null = null;
  successMessage: string | null = null;
  isGuardian = false;

  // Modal
  showApprovalModal = false;
  selectedApproval: GuardianPendingApprovalDto | null = null;

  approvalForm: FormGroup = this.fb.group({
    approved: [true],
    notes: ['', Validators.maxLength(500)]
  });

  ngOnInit(): void {
    this.isGuardian = this.authService.hasRole(Roles.Guardian);
    if (!this.isGuardian) {
      this.error = 'Access denied.';
      return;
    }

    this.loadData();
    this.guardianService.startSignalR();
  }

  loadData() {
    this.isLoading = true;
    this.guardianService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
        this.error = 'Failed to load dashboard data.';
        console.error(err);
      }
    });
  }

  // FIX: Correctly call service with (requestId, notes) instead of object
  approveRequest(requestId: number) {
    this.guardianService.approveContactRequest(requestId).subscribe(() => {
      this.successMessage = 'Request approved';
      this.loadData();
    });
  }

  scrollToSection(sectionId: string): void {
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }

  // FIX: Correctly call service with (requestId, reason)
  declineRequest(requestId: number) {
    if(confirm('Decline this request?')) {
      this.guardianService.declineContactRequest(requestId, 'Declined by guardian').subscribe(() => {
        this.successMessage = 'Request declined';
        this.loadData();
      });
    }
  }

  openApprovalModal(approval: GuardianPendingApprovalDto): void {
    this.selectedApproval = approval;
    this.approvalForm.reset({ approved: true });
    this.showApprovalModal = true;
  }

  closeApprovalModal(): void {
    this.showApprovalModal = false;
    this.selectedApproval = null;
  }

  submitApproval(): void {
    if (!this.selectedApproval) return;

    this.isProcessingApproval = true;
    const responseType = this.approvalForm.value.approved ? 'Accepted' : 'Declined';
    const notes = this.approvalForm.value.notes;

    // FIX: Pass arguments individually as Service expects
    this.guardianService.respondToScholarship(
      this.selectedApproval.offerId,
      responseType,
      notes
    ).subscribe({
      next: () => {
        this.isProcessingApproval = false;
        this.closeApprovalModal();
        this.successMessage = 'Response sent successfully';
        this.loadData();
      },
      error: (err) => {
        this.isProcessingApproval = false;
        this.error = 'Failed to submit response';
      }
    });
  }
}