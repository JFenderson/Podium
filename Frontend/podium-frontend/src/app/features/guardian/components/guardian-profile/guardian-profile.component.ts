import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { GuardianService } from '../../services/guardian.service';
import { GuardianDto } from '../../../../core/models/guardian.models';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-guardian-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './guardian-profile.component.html'
})
export class GuardianProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private guardianService = inject(GuardianService);

  profileForm: FormGroup;
  isLoading = true;
  isSaving = false;
  message: { type: 'success' | 'error', text: string } | null = null;

  constructor() {
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: [{ value: '', disabled: true }], // Email is usually immutable or handled separately
      phoneNumber: ['', [Validators.pattern(/^\+?1?\d{10,15}$/)]],
      emailNotificationsEnabled: [true],
      smsNotificationsEnabled: [false]
    });
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.guardianService.getProfile().subscribe({
      next: (profile: GuardianDto) => {
        this.profileForm.patchValue(profile);
        this.isLoading = false;
      },
      error: () => {
        this.message = { type: 'error', text: 'Failed to load profile.' };
        this.isLoading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.profileForm.invalid) return;

    this.isSaving = true;
    this.message = null;

    // 1. Update Profile Details
    const profileDto = {
      firstName: this.profileForm.get('firstName')?.value,
      lastName: this.profileForm.get('lastName')?.value,
      phoneNumber: this.profileForm.get('phoneNumber')?.value
    };

    // 2. Update Preferences (Separate API call based on your service structure, or combined if backend supports it)
    // We'll update profile first
    this.guardianService.updateProfile(profileDto)
      .pipe(
        // Chain the preference update
        // switchMap(() => this.guardianService.updateNotificationPreferences(...)) <-- If you want strictly sequential
        finalize(() => this.isSaving = false)
      )
      .subscribe({
        next: () => {
            // Also update preferences since they are on the same form
            const prefs = {
                emailEnabled: this.profileForm.get('emailNotificationsEnabled')?.value,
                smsEnabled: this.profileForm.get('smsNotificationsEnabled')?.value
            };
            this.guardianService.updateNotificationPreferences(prefs).subscribe(); // Fire and forget for now or chain it
            
            this.message = { type: 'success', text: 'Profile updated successfully.' };
        },
        error: () => {
          this.message = { type: 'error', text: 'Failed to update profile.' };
        }
      });
  }
}