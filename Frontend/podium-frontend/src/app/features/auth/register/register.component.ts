import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterRequest } from '../../../core/models/auth.models'; // Ensure this path matches

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html'
})
export class RegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  isSubmitting = false;
  showInstrumentField = false;

  registerForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: ['Student', Validators.required], // Default to Student
    instrument: ['']
  });

  ngOnInit() {
    // Watch for role changes to toggle instrument field
    this.registerForm.get('role')?.valueChanges.subscribe(role => {
      this.showInstrumentField = role === 'Student';
      
      const instrumentControl = this.registerForm.get('instrument');
      if (this.showInstrumentField) {
        instrumentControl?.setValidators(Validators.required);
      } else {
        instrumentControl?.clearValidators();
        instrumentControl?.setValue('');
      }
      instrumentControl?.updateValueAndValidity();
    });

    // Initialize visibility state
    this.showInstrumentField = this.registerForm.get('role')?.value === 'Student';
  }

  onSubmit() {
    if (this.registerForm.invalid) return;

    this.isSubmitting = true;
    
    // Cast form value to RegisterRequest interface
    const formValue = this.registerForm.value;
    const request: RegisterRequest = {
      firstName: formValue.firstName!,
      lastName: formValue.lastName!,
      email: formValue.email!,
      password: formValue.password!,
      role: formValue.role!,
      instrument: formValue.instrument || undefined
    };

    this.authService.register(request).subscribe({
      next: () => {
        this.toastr.success('Registration successful! Please login.');
        this.router.navigate(['/auth/login']);
      },
      error: (err) => {
        this.isSubmitting = false;
        // Handle backend array of errors if available
        if (err.error?.errors && Array.isArray(err.error.errors)) {
          err.error.errors.forEach((e: string) => this.toastr.error(e));
        } else {
          this.toastr.error(err.error?.message || 'Registration failed.');
        }
      }
    });
  }
}