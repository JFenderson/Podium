import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ScholarshipService } from '../../services/scholarship';
import { AuthService } from '../../../../features/auth/services/auth';
import { 
  ScholarshipOfferDto, 
  ScholarshipFilterDto,
} from '../../../../core/models/scholarship';
import { Permissions, PagedResult } from '../../../../core/models/common';

@Component({
  selector: 'app-scholarship-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './scholarship-list.html'
})
export class ScholarshipListComponent implements OnInit {
  private scholarshipService = inject(ScholarshipService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  Math = Math;
  offers: ScholarshipOfferDto[] = [];
  isLoading = false;
  error: string | null = null;
  canSendOffers = false;
  canViewOffers = false;

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  filterForm: FormGroup = this.fb.group({
    search: [''],
    status: [''],
    offerType: ['']
  });

  statuses = ['Draft', 'Sent', 'Accepted', 'Declined', 'Expired', 'Withdrawn'];
  offerTypes = ['Full Scholarship', 'Partial Scholarship', 'Stipend', 'Room & Board', 'Tuition Only', 'Other'];

  ngOnInit(): void {
    this.canSendOffers = this.authService.hasPermission(Permissions.SendOffers);
    this.canViewOffers = this.canSendOffers || this.authService.hasPermission(Permissions.ViewStudents);
    
    if (!this.canViewOffers) {
      this.error = 'Access denied. You do not have permission to view scholarship offers.';
      return;
    }

    this.loadOffers();
    this.setupFilterListeners();
  }

  loadOffers(): void {
    this.isLoading = true;
    this.error = null;

    const filter: ScholarshipFilterDto = {
      ...this.filterForm.value,
      pageNumber: this.currentPage,
      pageSize: this.pageSize
    };

    // Remove empty values
    Object.keys(filter).forEach(key => {
      if (!filter[key as keyof ScholarshipFilterDto]) {
        delete filter[key as keyof ScholarshipFilterDto];
      }
    });

    this.scholarshipService.getOffers(filter).subscribe({
      next: (result: PagedResult<ScholarshipOfferDto>) => {
        this.offers = result.items;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.isLoading = false;
      },
      error: (error) => {
        this.error = 'Failed to load scholarship offers. Please try again.';
        this.isLoading = false;
        console.error('Error loading offers:', error);
      }
    });
  }

  setupFilterListeners(): void {
    this.filterForm.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.currentPage = 1;
        this.loadOffers();
      });
  }

  clearFilters(): void {
    this.filterForm.reset();
    this.currentPage = 1;
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadOffers();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.goToPage(this.currentPage + 1);
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.goToPage(this.currentPage - 1);
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    
    let startPage = Math.max(1, this.currentPage - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);
    
    if (endPage - startPage < maxPagesToShow - 1) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    
    return pages;
  }

  getStatusColor(status: string): string {
    const colors: { [key: string]: string } = {
      'Draft': 'bg-gray-100 text-gray-800',
      'Sent': 'bg-blue-100 text-blue-800',
      'Accepted': 'bg-green-100 text-green-800',
      'Declined': 'bg-red-100 text-red-800',
      'Expired': 'bg-yellow-100 text-yellow-800',
      'Withdrawn': 'bg-orange-100 text-orange-800'
    };
    return colors[status] || 'bg-gray-100 text-gray-800';
  }

  getStatusIcon(status: string): string {
    const icons: { [key: string]: string } = {
      'Draft': 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z',
      'Sent': 'M12 19l9 2-9-18-9 18 9-2zm0 0v-8',
      'Accepted': 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
      'Declined': 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z',
      'Expired': 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
      'Withdrawn': 'M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6'
    };
    return icons[status] || icons['Draft'];
  }

  isExpiringSoon(expiresAt?: Date): boolean {
    if (!expiresAt) return false;
    const now = new Date();
    const expires = new Date(expiresAt);
    const daysUntilExpiry = Math.ceil((expires.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    return daysUntilExpiry <= 7 && daysUntilExpiry > 0;
  }

  isExpired(expiresAt?: Date): boolean {
    if (!expiresAt) return false;
    const now = new Date();
    const expires = new Date(expiresAt);
    return expires < now;
  }
}