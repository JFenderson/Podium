import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ScholarshipService } from '../../services/scholarship.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-create-offer',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './create-offer.component.html'
})
export class CreateOfferComponent {
  offerForm: FormGroup;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private scholarshipService: ScholarshipService,
    private router: Router,
    private toast: ToastService
  ) {
    this.offerForm = this.fb.group({
      studentId: ['', [Validators.required, Validators.min(1)]],
      bandId: [1, Validators.required], // TODO: Get from current user's band context
      amount: ['', [Validators.required, Validators.min(1)]],
      offerType: ['', Validators.required],
      description: ['', Validators.required],
      expiresAt: [null],
      requiresGuardianApproval: [true]
    });
  }

  onSubmit(): void {
    if (this.offerForm.invalid) return;

    // CONFIRMATION DIALOG
    const amount = this.offerForm.get('scholarshipAmount')?.value;
    if (!confirm(`Are you sure you want to send this offer of $${amount}? This action will be logged.`)) {
      return;
    }

    this.isSubmitting = true;
    this.scholarshipService.createOffer(this.offerForm.value).subscribe({
      next: (res) => {
        this.toast.success('Offer sent successfully!'); // Assuming ToastService has this
        this.router.navigate(['/director/dashboard']); // or back to list
      },
      error: (err) => {
        this.toast.error('Failed to create offer.');
        this.isSubmitting = false;
      }
    });
  }
}