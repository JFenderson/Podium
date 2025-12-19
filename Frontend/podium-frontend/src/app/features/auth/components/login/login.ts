import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { AuthService } from '../../services/auth';
import { LoginRequest } from '../../../../core/models/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './login.html',
  styleUrls: ['./login.scss']
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  isLoading = false;
  hidePassword = true;
error: string | null = null;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    // Redirect if already logged in
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
      return;
    }

    this.initForm();
  }

  /**
   * Initialize login form
   */
  private initForm(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  /**
   * Submit login form
   */
onSubmit(): void {
    if (this.loginForm.invalid) {
      this.markFormGroupTouched(this.loginForm);
      return;
    }

    this.isLoading = true;
    this.error = null;

    const { email, password } = this.loginForm.value;

    console.log('Attempting login...', { email });

    this.authService.login(email, password).subscribe({
      next: (response) => {
        console.log('Login successful!', response);
        this.isLoading = false;

        // Get the current user to determine redirect
        const user = this.authService.currentUserValue;
        console.log('Current user after login:', user);

        if (!user) {
          console.error('No user found after login!');
          this.error = 'Login failed - no user data received';
          return;
        }

        // Redirect based on primary role
        const primaryRole = user.roles && user.roles.length > 0 ? user.roles[0] : null;
        console.log('Primary role:', primaryRole);

        switch (primaryRole) {
          case 'Student':
            this.router.navigate(['/students/profile']);
            break;
          case 'Guardian':
            this.router.navigate(['/guardian/dashboard']);
            break;
          case 'Director':
          case 'BandStaff':
            this.router.navigate(['/director/dashboard']);
            break;
          default:
            this.router.navigate(['/dashboard']);
        }
      },
      error: (error) => {
        console.error('Login error:', error);
        this.isLoading = false;

        // Handle different error types
        if (error.status === 0) {
          this.error = 'Cannot connect to server. Please check your connection.';
        } else if (error.status === 401) {
          this.error = 'Invalid email or password';
        } else if (error.error?.message) {
          this.error = error.error.message;
        } else if (error.message) {
          this.error = error.message;
        } else {
          this.error = 'Login failed. Please try again.';
        }
      }
    });
  }

  /**
   * Navigate to appropriate page based on user role
   */
  private navigateBasedOnRole(): void {
    const user = this.authService.currentUserValue;
    
    if (!user) {
      this.router.navigate(['/dashboard']);
      return;
    }

    const primaryRole = user.roles[0]; // Get first role
    switch (primaryRole) {
      case 'Student':
        this.router.navigate(['/students/profile']);
        break;
      case 'Guardian':
        this.router.navigate(['/guardian/dashboard']);
        break;
      case 'BandStaff':
      case 'Director':
        this.router.navigate(['/director/dashboard']);
        break;
      default:
        this.router.navigate(['/dashboard']);
    }
  }

  /**
   * Mark all form fields as touched to show validation errors
   */
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  // Helper methods for template
  get emailControl() {
    return this.loginForm.get('email');
  }

  get passwordControl() {
    return this.loginForm.get('password');
  }

  /**
   * Get error message for email field
   */
  getEmailError(): string {
    const email = this.loginForm.get('email');
    if (email?.hasError('required')) {
      return 'Email is required';
    }
    if (email?.hasError('email')) {
      return 'Please enter a valid email';
    }
    return '';
  }

  /**
   * Get error message for password field
   */
  getPasswordError(): string {
    const password = this.loginForm.get('password');
    if (password?.hasError('required')) {
      return 'Password is required';
    }
    if (password?.hasError('minlength')) {
      return 'Password must be at least 6 characters';
    }
    return '';
  }
}