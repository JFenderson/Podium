import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

// Define the supported statuses
export type StudentAccountStatus = 'active' | 'guardian_required' | 'pending' | 'incomplete';

interface BadgeConfig {
  label: string;
  classes: string;
  iconPath: string; // SVG path data
  tooltip: string;
}

@Component({
  selector: 'app-student-status-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-status-badge.component.html',
  styles: [] // We are using Tailwind, so no external CSS needed
})
export class StudentStatusBadgeComponent implements OnChanges {
  /**
   * Directly sets the status type. 
   * If provided, overrides the boolean calculation logic.
   */
  @Input() status?: StudentAccountStatus;

  /**
   * Flag from Student/InterestedStudent DTOs.
   * If true, sets status to 'guardian_required'.
   */
  @Input() requiresGuardianApproval = false;

  /**
   * Flag for generic pending states (e.g., waiting for email verification).
   */
  @Input() isPending = false;

  /**
   * Flag to check if profile is fully filled out.
   */
  @Input() isProfileComplete = true;

  // Internal effective status used for rendering
  public currentStatus: StudentAccountStatus = 'active';
  
  // Configuration for rendering logic
  protected config: Record<StudentAccountStatus, BadgeConfig> = {
    active: {
      label: 'Active',
      classes: 'bg-green-100 text-green-800 border-green-200',
      iconPath: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', // Check Circle
      tooltip: 'Student account is active and verified.'
    },
    guardian_required: {
      label: 'Guardian Approval Required',
      classes: 'bg-amber-100 text-amber-800 border-amber-200 ring-1 ring-amber-300', // Prominent styling
      iconPath: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z', // Warning Triangle
      tooltip: 'This student is a minor and requires a guardian to approve their account before full access is granted.'
    },
    pending: {
      label: 'Pending',
      classes: 'bg-blue-100 text-blue-800 border-blue-200',
      iconPath: 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z', // Clock
      tooltip: 'Account is pending verification.'
    },
    incomplete: {
      label: 'Incomplete',
      classes: 'bg-gray-100 text-gray-800 border-gray-200',
      iconPath: 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z', // Info
      tooltip: 'Profile information is missing.'
    }
  };

  ngOnChanges(changes: SimpleChanges): void {
    this.calculateStatus();
  }

  private calculateStatus(): void {
    // 1. Priority: Explicit status input
    if (this.status) {
      this.currentStatus = this.status;
      return;
    }

    // 2. Priority: Guardian Approval (Based on your interestedStudentDto logic)
    if (this.requiresGuardianApproval) {
      this.currentStatus = 'guardian_required';
      return;
    }

    // 3. Priority: Generic Pending
    if (this.isPending) {
      this.currentStatus = 'pending';
      return;
    }

    // 4. Priority: Incomplete Profile
    if (!this.isProfileComplete) {
      this.currentStatus = 'incomplete';
      return;
    }

    // 5. Default
    this.currentStatus = 'active';
  }

  get currentConfig(): BadgeConfig {
    return this.config[this.currentStatus];
  }
}