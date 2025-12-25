import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { GuardianService } from '../../services/guardian.service';
import { AuthService } from '../../../../features/auth/services/auth.service';
import {
  GuardianPendingApprovalDto,
  GuardianApprovalDto
} from '../../../../core/models/guardian.models';
import { Roles } from '../../../../core/models/common.models';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { ScholarshipCardComponent } from '../scholarship-card/scholarship-card.component';
import { ContactRequestCardComponent } from '../contact-request-card/contact-request-card.component';

@Component({
  selector: 'app-guardian-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    SkeletonLoaderComponent,
    
],
  templateUrl: './guardian-dashboard.component.html'
})
export class GuardianDashboardComponent implements OnInit {
  private guardianService = inject(GuardianService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  // Assuming guardianService.dashboard is a Signal. 
  // If it's a BehaviorSubject, use AsyncPipe in template.
  dashboard = this.guardianService.dashboard; 
  
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

    this.loadData();
    // Ensure SignalR is connected for real-time updates
    this.guardianService.startSignalR(); 
  }

  loadData() {
    this.isLoading = true;
    this.guardianService.getDashboard().subscribe({
      next: () => this.isLoading = false,
      error: (err) => {
        this.isLoading = false;
        this.error = 'Failed to load dashboard data.';
        console.error(err);
      }
    });
  }

  // --- Handlers ---

  approveRequest(requestId: number) {
    this.guardianService.approveContactRequest({ requestId, approved: true }).subscribe();
  }

  declineRequest(requestId: number) {
    if(confirm('Are you sure you want to decline this request?')) {
      this.guardianService.approveContactRequest({ requestId, approved: false }).subscribe();
    }
  }

  openApprovalModal(approval: GuardianPendingApprovalDto): void {
    this.selectedApproval = approval;
    // Default to approved = true
    this.approvalForm.reset({ approved: true, notes: '' });
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

    const responseType = this.approvalForm.value.approved ? 'Accepted' : 'Declined';
    const notes = this.approvalForm.value.notes;

    // Use the offerId from the selected approval
    this.guardianService.respondToScholarship({
      offerId: this.selectedApproval.offerId, 
      status: responseType, 
      // notes: notes // Uncomment if backend DTO accepts notes for scholarship response
    }).subscribe({
      next: () => {
        this.isProcessingApproval = false;
        this.closeApprovalModal();
        this.successMessage = this.approvalForm.value.approved 
          ? 'Offer approved successfully!' 
          : 'Offer declined successfully.';
        
        // Reload data to reflect changes
        this.loadData();
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (error: any) => {
        this.error = 'Failed to process approval. Please try again.';
        this.isProcessingApproval = false;
        console.error('Error processing approval:', error);
      }
    });
  }

  getUrgencyClass(expiresAt?: Date): string {
    if (!expiresAt) return 'border-gray-200';
    
    const now = new Date();
    const expires = new Date(expiresAt);
    const diffTime = expires.getTime() - now.getTime();
    const daysUntilExpiry = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    
    if (daysUntilExpiry <= 3) {
      return 'border-red-500 bg-red-50';
    } else if (daysUntilExpiry <= 7) {
      return 'border-yellow-500 bg-yellow-50';
    }
    return 'border-gray-200';
  }
}