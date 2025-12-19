import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ScholarshipService } from '../../services/scholarship';
import { StudentService } from '../../../student/services/student';
import { AuthService } from '../../../../features/auth/services/auth';
import { CreateScholarshipOfferDto } from '../../../../core/models/scholarship';
import { StudentDetailsDto } from '../../../../core/models/student';
import { Permissions } from '../../../../core/models/common';

@Component({
  selector: 'app-scholarship-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './scholarship-create.html'
})
export class ScholarshipCreateComponent implements OnInit {
  private scholarshipService = inject(ScholarshipService);
  private studentService = inject(StudentService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  offerForm: FormGroup = this.fb.group({
    studentId: [null, Validators.required],
    offerType: ['', Validators.required],
    amount: [null, [Validators.required, Validators.min(0)]],
    duration: [''],
    description: ['', Validators.maxLength(2000)],
    terms: ['', Validators.maxLength(5000)],
    expiresAt: [''],
    requiresGuardianApproval: [true]
  });

  students: StudentDetailsDto[] = [];
  isLoading = false;
  isSubmitting = false;
  error: string | null = null;
  canSendOffers = false;

  offerTypes = [
    'Full Scholarship',
    'Partial Scholarship',
    'Stipend',
    'Room & Board',
    'Tuition Only',
    'Books & Fees',
    'Other'
  ];

  durations = [
    '1 Semester',
    '1 Year',
    '2 Years',
    '3 Years',
    '4 Years',
    'Full Academic Career'
  ];

  ngOnInit(): void {
    this.canSendOffers = this.authService.hasPermission(Permissions.SendOffers);
    
    if (!this.canSendOffers) {
      this.error = 'Access denied. You do not have permission to create scholarship offers.';
      return;
    }

    this.loadStudents();
    
    // Set default expiry date to 30 days from now
    const defaultExpiry = new Date();
    defaultExpiry.setDate(defaultExpiry.getDate() + 30);
    this.offerForm.patchValue({
      expiresAt: defaultExpiry.toISOString().split('T')[0]
    });
  }

  loadStudents(): void {
    this.isLoading = true;
    
    this.studentService.getStudents({ pageSize: 100, pageNumber: 1 }).subscribe({
      next: (result) => {
        this.students = result.items;
        this.isLoading = false;
      },
      error: (error) => {
        this.error = 'Failed to load students. Please try again.';
        this.isLoading = false;
        console.error('Error loading students:', error);
      }
    });
  }

  onStudentChange(event: any): void {
    const studentId = parseInt(event.target.value);
    const student = this.students.find(s => s.studentId === studentId);
    
    if (student && student.hasGuardian) {
      this.offerForm.patchValue({ requiresGuardianApproval: true });
    }
  }

  createOffer(): void {
    if (this.offerForm.invalid) {
      this.markFormGroupTouched(this.offerForm);
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const currentUser = this.authService.currentUserValue;
    if (!currentUser?.bandId) {
      this.error = 'Band information not found';
      this.isSubmitting = false;
      return;
    }

    const dto: CreateScholarshipOfferDto = {
      ...this.offerForm.value,
      bandId: currentUser.bandId,
      status: 'Draft'
    };

    // Convert date to ISO format if provided
    if (dto.expiresAt) {
      dto.expiresAt = new Date(dto.expiresAt).toISOString();
    }

    this.scholarshipService.createOffer(dto).subscribe({
      next: (offer) => {
        this.isSubmitting = false;
        this.router.navigate(['/scholarships', offer.offerId]);
      },
      error: (error) => {
        this.error = 'Failed to create offer. Please try again.';
        this.isSubmitting = false;
        console.error('Error creating offer:', error);
      }
    });
  }

  saveDraft(): void {
    if (this.offerForm.get('studentId')?.invalid || this.offerForm.get('offerType')?.invalid) {
      this.markFormGroupTouched(this.offerForm);
      this.error = 'Student and Offer Type are required';
      return;
    }

    // Create as draft without full validation
    this.createOffer();
  }

  cancel(): void {
    this.router.navigate(['/scholarships']);
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }
}