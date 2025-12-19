import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { BandStaffService } from '../../services/band-staff';
import { AuthService } from '../../../auth/services/auth';
import { BandStaffDto, CreateBandStaffDto, BandStaffPermissionsDto } from '../../../../core/models/band-staff';
import { Roles } from '../../../../core/models/common';

@Component({
  selector: 'app-staff-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './band-staff-list.html'
})
export class StaffListComponent implements OnInit {
  private bandStaffService = inject(BandStaffService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  staffMembers: BandStaffDto[] = [];
  isLoading = false;
  error: string | null = null;
  isDirector = false;

  // Modal states
  showAddModal = false;
  showEditModal = false;
  selectedStaff: BandStaffDto | null = null;

  // Forms
  addStaffForm: FormGroup = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    role: ['Assistant Director', Validators.required],
    canViewStudents: [true],
    canRateStudents: [false],
    canSendOffers: [false],
    canContactStudents: [true],
    canManageEvents: [false],
    canManageStaff: [false]
  });

  permissionsForm: FormGroup = this.fb.group({
    canViewStudents: [false],
    canRateStudents: [false],
    canSendOffers: [false],
    canContactStudents: [false],
    canManageEvents: [false],
    canManageStaff: [false]
  });

  ngOnInit(): void {
    this.isDirector = this.authService.hasRole(Roles.Director);
    
    if (!this.isDirector) {
      this.error = 'Access denied. Director privileges required.';
      return;
    }

    this.loadStaff();
  }

  loadStaff(): void {
    this.isLoading = true;
    this.error = null;

    this.bandStaffService.getAllStaff().subscribe({
      next: (staff) => {
        this.staffMembers = staff;
        this.isLoading = false;
      },
      error: (error) => {
        this.error = 'Failed to load staff members. Please try again.';
        this.isLoading = false;
        console.error('Error loading staff:', error);
      }
    });
  }

  openAddModal(): void {
    this.addStaffForm.reset({
      role: 'Assistant Director',
      canViewStudents: true,
      canContactStudents: true
    });
    this.showAddModal = true;
  }

  closeAddModal(): void {
    this.showAddModal = false;
    this.addStaffForm.reset();
  }

  addStaff(): void {
    if (this.addStaffForm.invalid) {
      this.markFormGroupTouched(this.addStaffForm);
      return;
    }

    const currentUser = this.authService.currentUserValue;
    if (!currentUser?.bandId) {
      this.error = 'Band information not found';
      return;
    }

    const dto: CreateBandStaffDto = {
      ...this.addStaffForm.value,
      bandId: currentUser.bandId
    };

    this.bandStaffService.createStaff(dto).subscribe({
      next: () => {
        this.closeAddModal();
        this.loadStaff();
        this.showSuccess('Staff member added successfully');
      },
      error: (error) => {
        this.error = 'Failed to add staff member. Please try again.';
        console.error('Error adding staff:', error);
      }
    });
  }

  openEditPermissions(staff: BandStaffDto): void {
    this.selectedStaff = staff;
    this.permissionsForm.patchValue({
      canViewStudents: staff.canViewStudents,
      canRateStudents: staff.canRateStudents,
      canSendOffers: staff.canSendOffers,
      canContactStudents: staff.canContactStudents,
      canManageEvents: staff.canManageEvents,
      canManageStaff: staff.canManageStaff
    });
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedStaff = null;
    this.permissionsForm.reset();
  }

  updatePermissions(): void {
    if (!this.selectedStaff) return;

    const permissions: BandStaffPermissionsDto = this.permissionsForm.value;

    this.bandStaffService.updatePermissions(this.selectedStaff.bandStaffId, permissions).subscribe({
      next: () => {
        this.closeEditModal();
        this.loadStaff();
        this.showSuccess('Permissions updated successfully');
      },
      error: (error) => {
        this.error = 'Failed to update permissions. Please try again.';
        console.error('Error updating permissions:', error);
      }
    });
  }

  promoteToDirector(staff: BandStaffDto): void {
    if (!confirm(`Promote ${staff.firstName} ${staff.lastName} to Director? This will grant full administrative access.`)) {
      return;
    }

    this.bandStaffService.updateStaff(staff.bandStaffId, {
      role: 'Director',
      canManageStaff: true,
      canManageEvents: true,
      canSendOffers: true,
      canViewStudents: true,
      canRateStudents: true,
      canContactStudents: true
    }).subscribe({
      next: () => {
        this.loadStaff();
        this.showSuccess('Staff member promoted to Director');
      },
      error: (error) => {
        this.error = 'Failed to promote staff member. Please try again.';
        console.error('Error promoting staff:', error);
      }
    });
  }

  removeStaff(staff: BandStaffDto): void {
    if (!confirm(`Remove ${staff.firstName} ${staff.lastName} from the band? This action cannot be undone.`)) {
      return;
    }

    this.bandStaffService.deleteStaff(staff.bandStaffId).subscribe({
      next: () => {
        this.loadStaff();
        this.showSuccess('Staff member removed successfully');
      },
      error: (error) => {
        this.error = 'Failed to remove staff member. Please try again.';
        console.error('Error removing staff:', error);
      }
    });
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }

  private showSuccess(message: string): void {
    // In a real app, use a toast service
    alert(message);
  }
}