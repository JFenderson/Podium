import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { StudentService } from '../../services/student.service';
import { AuthService } from '../../../auth/services/auth.service';
import { StudentDetailsDto, StudentFilterDto, PagedResult } from '../../../../core/models/student.models';
import { Permissions } from '../../../../core/models/common.models';
import { StudentStatusBadgeComponent } from '../student-status-badge/student-status-badge.component';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, StudentStatusBadgeComponent],
  templateUrl: './student-list.component.html'
})
export class StudentListComponent implements OnInit {
  private studentService = inject(StudentService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
Math = Math;
  students: StudentDetailsDto[] = [];
  isLoading = false;
  error: string | null = null;
  canViewStudents = false;

  // Pagination
  currentPage = 1;
  pageSize = 12;
  totalCount = 0;
  totalPages = 0;

  // Filters
  instruments: string[] = ['Trumpet', 'Trombone', 'Tuba', 'Saxophone', 'Clarinet', 'Flute', 'Percussion', 'Mellophone', 'Baritone'];
  graduationYears: number[] = [];

  filterForm: FormGroup = this.fb.group({
    search: [''],
    instrument: [''],
    graduationYear: [''],
    minGpa: [''],
    state: ['']
  });

  ngOnInit(): void {
    this.canViewStudents = this.authService.hasPermission(Permissions.ViewStudents);
    
    if (!this.canViewStudents) {
      this.error = 'Access denied. You do not have permission to view students.';
      return;
    }

    this.initializeGraduationYears();
    this.loadStudents();
    this.setupFilterListeners();
  }

  initializeGraduationYears(): void {
    const currentYear = new Date().getFullYear();
    for (let i = 0; i < 5; i++) {
      this.graduationYears.push(currentYear + i);
    }
  }

  loadStudents(): void {
    this.isLoading = true;
    this.error = null;

    const filter: StudentFilterDto = {
      ...this.filterForm.value,
      pageNumber: this.currentPage,
      pageSize: this.pageSize
    };

    // Remove empty values
    Object.keys(filter).forEach(key => {
      if (!filter[key as keyof StudentFilterDto]) {
        delete filter[key as keyof StudentFilterDto];
      }
    });

    this.studentService.getStudents(filter).subscribe({
      next: (result: PagedResult<StudentDetailsDto>) => {
        this.students = result.items;
        this.totalCount = result.totalCount;
        this.totalPages = result.totalPages;
        this.isLoading = false;
      },
      error: (error: any) => {
        this.error = 'Failed to load students. Please try again.';
        this.isLoading = false;
        console.error('Error loading students:', error);
      }
    });
  }

  setupFilterListeners(): void {
    this.filterForm.valueChanges
      .pipe(
        debounceTime(500),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.currentPage = 1;
        this.loadStudents();
      });
  }

  clearFilters(): void {
    this.filterForm.reset();
    this.currentPage = 1;
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadStudents();
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

  getRatingStars(rating: number | undefined): string {
    if (!rating) return '☆☆☆☆☆';
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 >= 0.5;
    let stars = '★'.repeat(fullStars);
    if (hasHalfStar) stars += '⯨';
    stars += '☆'.repeat(5 - fullStars - (hasHalfStar ? 1 : 0));
    return stars;
  }
}