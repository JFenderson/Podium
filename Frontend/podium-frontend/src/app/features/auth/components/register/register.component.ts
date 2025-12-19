import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common'; // needed for AsyncPipe or @if
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss' // Optional if you have styles
})
export class RegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  registerForm: FormGroup;
  isLoading = signal(false);
  error = signal<string | null>(null);

  // Signals to hold backend options
  bands = signal<{ id: number; bandName: string }[]>([]);
  roles = signal<string[]>([]);

  constructor() {
    this.registerForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
      bandId: [null, Validators.required], // Now a dropdown
      role: ['', Validators.required]      // Now a dropdown
    });
  }

  ngOnInit(): void {
    this.loadOptions();
  }

  loadOptions(): void {
    this.authService.getRegistrationOptions().subscribe({
      next: (data) => {
        this.bands.set(data.bands);
        this.roles.set(data.roles);
      },
      error: (err) => console.error('Failed to load options', err)
    });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) return;

    this.isLoading.set(true);
    this.error.set(null);

    this.authService.register(this.registerForm.value).subscribe({
      next: () => {
        // Registration successful
        this.router.navigate(['/login']); 
        // Optional: Show success message/toast here
      },
      error: (err) => {
        this.error.set(err.message || 'Registration failed');
        this.isLoading.set(false);
      }
    });
  }
}