import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { GuardianService } from '../../services/guardian.service';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-link-student',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './link-student.component.html',
  // styleUrls: ['./link-student.component.css']
})
export class LinkStudentComponent {
  linkForm: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  constructor(
    private fb: FormBuilder,
    private guardianService: GuardianService,
    private router: Router
  ) {
    this.linkForm = this.fb.group({
      studentEmail: ['', [Validators.required, Validators.email]],
      relationship: ['Parent', [Validators.required]],
      verificationCode: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  onSubmit(): void {
    if (this.linkForm.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const dto = this.linkForm.value;

    this.guardianService.linkStudent(dto)
      .pipe(finalize(() => this.isLoading = false))
      .subscribe({
        next: (response) => {
          this.successMessage = 'Student linked successfully!';
          this.linkForm.reset({ relationship: 'Parent' });
          
          // Optional: Redirect back to dashboard after a delay
          setTimeout(() => {
            this.router.navigate(['/guardian/dashboard']);
          }, 1500);
        },
        error: (error) => {
          // Handle backend error messages (e.g., "Student email not found")
          this.errorMessage = error.error?.message || 'Failed to link student. Please verify the email address.';
        }
      });
  }
}