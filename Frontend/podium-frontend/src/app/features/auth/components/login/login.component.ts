import { Component, OnInit, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../auth/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit {
  // Initialize immediately to prevent HTML errors
  loginForm: FormGroup;
  isLoading = false;
  hidePassword = true;
  error: string | null = null;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private ngZone: NgZone
  ) {
    // Form is created instantly when component is instantiated
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  ngOnInit(): void {
    // Redirect if already logged in
    if (this.authService.isAuthenticated()) {
      this.navigateBasedOnRole();
    }
  }

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
      next: (response: any) => {
        console.log('✅ Login successful!', response);
        // Small delay to ensure token is set
        setTimeout(() => {
          this.isLoading = false;
          this.navigateBasedOnRole();
        }, 100);
      },
      error: (error: any) => {
        console.error('❌ Login error:', error);
        this.isLoading = false;

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
      },
    });
  }

  private navigateBasedOnRole(): void {
    const user = this.authService.currentUserValue;
    
    // If no user/roles found, default to dashboard
    if (!user || !user.roles || user.roles.length === 0) {
      this.navigateToRoute('/dashboard');
      return;
    }

    const primaryRole = user.roles[0];
    let targetRoute: string;

    switch (primaryRole) {
      case 'Student':
        targetRoute = '/student/dashboard';
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
    this.navigateToRoute(targetRoute);
  }

  private navigateToRoute(route: string): void {
    this.ngZone.run(() => {
      this.router.navigate([route], { replaceUrl: true }).catch((error) => {
        console.error('❌ Navigation error:', error);
        // Fallback if router fails
        window.location.href = route;
      });
    });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach((key) => {
      const control = formGroup.get(key);
      control?.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  get emailControl() { return this.loginForm.get('email'); }
  get passwordControl() { return this.loginForm.get('password'); }

  getEmailError(): string {
    const email = this.loginForm.get('email');
    if (email?.hasError('required')) return 'Email is required';
    if (email?.hasError('email')) return 'Please enter a valid email';
    return '';
  }

  getPasswordError(): string {
    const password = this.loginForm.get('password');
    if (password?.hasError('required')) return 'Password is required';
    if (password?.hasError('minlength')) return 'Password must be at least 6 characters';
    return '';
  }
}