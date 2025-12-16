import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ScholarshipService } from '../../../core/services/scholarship.service';
import { ScholarshipBudgetDto, ScholarshipOfferDto } from '../../../core/models/scholarship.models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-director-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './director-dashboard.component.html'
})
export class DirectorDashboardComponent implements OnInit {
  private scholarshipService = inject(ScholarshipService);
  private fb = inject(FormBuilder);
  private toastr = inject(ToastrService);

  // State
  budget: ScholarshipBudgetDto | null = null;
  recentOffers: ScholarshipOfferDto[] = [];
  isLoading = true;
  showModal = false;
  
  // Hardcoded for demo - in reality, get from AuthService.currentUser
  currentBandId = 1; 

  // New Offer Form
  offerForm = this.fb.group({
    studentId: ['', [Validators.required]], // Usually a dropdown/search
    amount: ['', [Validators.required, Validators.min(1)]],
    type: ['Scholarship', Validators.required],
    expiration: ['', Validators.required]
  });

  ngOnInit() {
    this.loadDashboardData();
  }

  loadDashboardData() {
    this.isLoading = true;
    // 1. Fetch Budget
    this.scholarshipService.getBudget(this.currentBandId).subscribe({
      next: (data) => this.budget = data,
      error: () => this.toastr.error('Failed to load budget data')
    });

    // 2. Fetch Recent Offers
    this.scholarshipService.getAllOffers({ pageSize: 10, pageIndex: 1 }).subscribe({
      next: (data) => {
        this.recentOffers = data.offers;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastr.error('Failed to load offers');
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Accepted': return 'bg-green-100 text-green-800';
      case 'Pending': return 'bg-yellow-100 text-yellow-800';
      case 'Declined': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  openNewOfferModal() {
    this.showModal = true;
    this.offerForm.reset({ type: 'Scholarship' });
  }

  closeModal() {
    this.showModal = false;
  }

  submitOffer() {
    if (this.offerForm.invalid) return;

    const val = this.offerForm.value;
    const dto = {
      studentId: Number(val.studentId),
      bandId: this.currentBandId,
      offerType: val.type!,
      scholarshipAmount: Number(val.amount),
      expirationDate: val.expiration!
    };

    this.scholarshipService.createOffer(dto).subscribe({
      next: () => {
        this.toastr.success('Offer created successfully!');
        this.closeModal();
        this.loadDashboardData(); // Refresh data
      },
      error: (err) => this.toastr.error('Failed to create offer')
    });
  }
}