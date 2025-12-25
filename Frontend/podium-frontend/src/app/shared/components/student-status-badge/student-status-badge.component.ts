import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StudentAccountStatus } from '../../../core/models/student-status.models';

@Component({
  selector: 'app-student-status-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-status-badge.component.html'
})
export class StudentStatusBadgeComponent {
  // Input signals for reactive updates
  status = signal<string>(StudentAccountStatus.Pending);
  isMinor = signal<boolean>(false);
  guardianLinked = signal<boolean>(false);
  requiresGuardianApproval = signal<boolean>(false);
  isPending = signal<boolean>(false);

  @Input('status') set setStatus(val: string) {
    this.status.set(val);
  }

  @Input('isMinor') set setIsMinor(val: boolean) {
    this.isMinor.set(val);
  }

  @Input('guardianLinked') set setGuardianLinked(val: boolean) {
    this.guardianLinked.set(val);
  }

  // FIX: Add the input expected by StudentListComponent
  @Input('requiresGuardianApproval') set setRequiresGuardianApproval(val: boolean) {
    this.requiresGuardianApproval.set(val);
  }

  // FIX: Add the input expected by StudentListComponent
  @Input('isPending') set setIsPending(val: boolean) {
    this.isPending.set(val);
  }

  // Computed properties for UI logic
  badgeConfig = computed(() => {
    const currentStatus = this.status();
    const isMinor = this.isMinor();
    const hasGuardian = this.guardianLinked();

    // Priority 1: Minor without Guardian (Overrides all others)
    if (isMinor && !hasGuardian) {
      return {
        label: 'Guardian Approval Required',
        icon: 'shield-exclamation',
        colorClass: 'bg-red-100 text-red-800 border-red-200',
        tooltip: 'This account belongs to a minor and requires a linked guardian to be fully active.',
        ariaLabel: 'Status: Guardian Approval Required'
      };
    }

    // Standard Status Mapping
    switch (currentStatus) {
      case StudentAccountStatus.Active:
        return {
          label: 'Active',
          icon: 'check-circle',
          colorClass: 'bg-green-100 text-green-800 border-green-200',
          tooltip: 'Account is fully active and visible to recruiters.',
          ariaLabel: 'Status: Active'
        };
      
      case StudentAccountStatus.Pending:
        return {
          label: 'Pending',
          icon: 'clock',
          colorClass: 'bg-yellow-100 text-yellow-800 border-yellow-200',
          tooltip: 'Account is pending verification.',
          ariaLabel: 'Status: Pending'
        };

      case StudentAccountStatus.Suspended:
        return {
          label: 'Suspended',
          icon: 'ban',
          colorClass: 'bg-gray-100 text-gray-800 border-gray-200',
          tooltip: 'Account has been suspended. Contact support.',
          ariaLabel: 'Status: Suspended'
        };
        
      case StudentAccountStatus.AwaitingGuardian:
        return {
          label: 'Awaiting Guardian',
          icon: 'user-group',
          colorClass: 'bg-orange-100 text-orange-800 border-orange-200',
          tooltip: 'Waiting for guardian to accept the link request.',
          ariaLabel: 'Status: Awaiting Guardian'
        };

      default:
        return {
          label: currentStatus,
          icon: 'information-circle',
          colorClass: 'bg-gray-100 text-gray-600 border-gray-200',
          tooltip: `Current status is ${currentStatus}`,
          ariaLabel: `Status: ${currentStatus}`
        };
    }
  });
}