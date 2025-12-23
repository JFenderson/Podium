import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ScholarshipService } from '../../services/scholarship.service';
import { AuthService } from '../../../auth/services/auth.service';
import { 
  ScholarshipOfferDto, 
  ScholarshipOfferStatus as OfferStatus,
  OfferType
} from '../../../../core/models/scholarship.models';
import { Roles } from '../../../../core/models/common.models';

@Component({
  selector: 'app-scholarship-list',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './scholarship-list.component.html'
})
export class ScholarshipListComponent implements OnInit {
  offers: ScholarshipOfferDto[] = [];
  filteredOffers: ScholarshipOfferDto[] = [];
  paginatedOffers: ScholarshipOfferDto[] = [];
  Math = Math; // For template use
  isLoading = false;
  filterForm: FormGroup;
  OfferStatus = OfferStatus; 
  // FIX: Added helper arrays for the Template Dropdowns
  statuses = Object.values(OfferStatus);
  offerTypes = Object.values(OfferType);
error: string | null = null;
  // Pagination Properties
  currentPage = 1;
  pageSize = 10;
  totalPages = 0;
  totalCount = 0;

  // Permission Flags
  canViewOffers = false;
  canSendOffers = false;

  constructor(
    private scholarshipService: ScholarshipService,
    private authService: AuthService,
    private fb: FormBuilder
  ) {
    this.filterForm = this.fb.group({
      search: [''],
      status: [''],
      type: ['']
    });
  }

  ngOnInit(): void {
    this.checkPermissions();

    this.filterForm.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage = 1;
      this.applyFilters();
    });

    this.loadOffers();
  }

  // FIX: Added Reset Method
  clearFilters(): void {
    this.filterForm.reset({
      search: '',
      status: '',
      type: ''
    });
  }

  checkPermissions(): void {
    const user = this.authService.currentUserValue;
    if (!user) return;

    if (user.roles.includes(Roles.Director) || user.roles.includes(Roles.BandStaff)) {
      this.canViewOffers = true;
      this.canSendOffers = true;
    } else if (user.roles.includes(Roles.Guardian)) {
      this.canViewOffers = true;
    }
  }

  loadOffers(): void {
    this.isLoading = true;
    this.error = null;
    this.scholarshipService.getMyOffers().subscribe({
      next: (data) => {
        this.offers = data;
        this.applyFilters();
        this.isLoading = false;
      },
      error: (err) => {
      console.error('Error loading offers', err);
        this.error = 'Failed to load scholarship offers. Please try again.'; // Set error message
        this.isLoading = false;
      }
    });
  }

  applyFilters(): void {
    const { search, status, type } = this.filterForm.value;
    
    this.filteredOffers = this.offers.filter(offer => {
      const matchesSearch = !search || 
        offer.studentName?.toLowerCase().includes(search.toLowerCase()) || 
        offer.offerType.toLowerCase().includes(search.toLowerCase());
      
      const matchesStatus = !status || offer.status === status;
      const matchesType = !type || offer.offerType === type;

      return matchesSearch && matchesStatus && matchesType;
    });

    this.totalCount = this.filteredOffers.length;
    this.totalPages = Math.ceil(this.totalCount / this.pageSize);
    this.updatePaginatedData();
  }

  updatePaginatedData(): void {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.paginatedOffers = this.filteredOffers.slice(startIndex, endIndex);
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updatePaginatedData();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updatePaginatedData();
    }
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.updatePaginatedData();
  }

  getPageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  isExpiringSoon(date?: Date): boolean {
    if (!date) return false;
    const expiry = new Date(date);
    const now = new Date();
    const diffTime = expiry.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays > 0 && diffDays <= 7;
  }

  isExpired(date?: Date): boolean {
    if (!date) return false;
    return new Date(date) < new Date();
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case OfferStatus.Accepted: return 'bg-green-100 text-green-800';
      case OfferStatus.Declined: return 'bg-red-100 text-red-800';
      case OfferStatus.Withdrawn: return 'bg-gray-100 text-gray-800';
      case OfferStatus.Sent: return 'bg-blue-100 text-blue-800';
      case OfferStatus.PendingGuardianSignature: return 'bg-yellow-100 text-yellow-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  getStatusIcon(status: string): string {
    switch (status) {
      case OfferStatus.Accepted:
        return 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z';
      case OfferStatus.Declined:
        return 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z';
      case OfferStatus.Sent:
        return 'M12 19l9 2-9-18-9 18 9-2zm0 0v-8';
      case OfferStatus.PendingGuardianSignature:
        return 'M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z';
      default:
        return 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z';
    }
  }
}