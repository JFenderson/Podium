import { Component, OnInit, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';


import { AuthService } from '../../../auth/services/auth.service';
import { LoginRequest } from '../../../../core/models/auth.models';

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
    MatSnackBarModule,
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
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
    private snackBar: MatSnackBar,
    private ngZone: NgZone
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
      password: ['', [Validators.required, Validators.minLength(6)]],
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

    console.log('🔵 Attempting login...', { email });

    this.authService.login(email, password).subscribe({
      next: (response) => {
        console.log('✅ Login successful!', response);

        // Use setTimeout to ensure auth state is fully updated
        setTimeout(() => {
          this.isLoading = false;
          this.navigateBasedOnRole();
        }, 100);
      },
      error: (error) => {
        console.error('❌ Login error:', error);
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

        // Only show snackbar if error message exists
        if (this.error) {
          this.snackBar.open(this.error, 'Close', {
            duration: 5000,
            horizontalPosition: 'center',
            verticalPosition: 'top',
            panelClass: ['error-snackbar'],
          });
        }
      },
    });
  }

  /**
   * Navigate to appropriate page based on user role
   */
  private navigateBasedOnRole(): void {
    const user = this.authService.currentUserValue;
    console.log('🔵 Current user after login:', user);

    if (!user || !user.roles || user.roles.length === 0) {
      console.log('⚠️ No user or roles found, navigating to dashboard');
      this.navigateToRoute('/dashboard');
      return;
    }

    const primaryRole = user.roles[0]; // Get first role
    console.log('🔵 Primary role:', primaryRole);

    let targetRoute: string;

    switch (primaryRole) {
      case 'Student':
        targetRoute = '/students/profile';
        break;
      case 'Guardian':
        targetRoute = '/guardian/dashboard';
        break;
      case 'BandStaff':
      case 'Director':
        targetRoute = '/director/dashboard';
        break;
      default:
        targetRoute = '/dashboard';
    }

    console.log('🔵 Navigating to:', targetRoute);
    this.navigateToRoute(targetRoute);
  }

  /**
   * Navigate to route with error handling
   */
  private navigateToRoute(route: string): void {
    // Run navigation inside Angular zone
    this.ngZone.run(() => {
      this.router
        .navigate([route], { replaceUrl: true })
        .then((success) => {
          if (success) {
            console.log('✅ Navigation successful to:', route);
          } else {
            console.error('❌ Navigation failed to:', route);
            // Try fallback
            if (route !== '/dashboard') {
              console.log('🔵 Attempting fallback to /dashboard');
              this.router.navigate(['/dashboard'], { replaceUrl: true });
            }
          }
        })
        .catch((error) => {
          console.error('❌ Navigation error:', error);
          // Last resort - use window.location
          console.log('🔵 Using window.location as fallback');
          window.location.href = route;
        });
    });
  }

  /**
   * Mark all form fields as touched to show validation errors
   */
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach((key) => {
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
