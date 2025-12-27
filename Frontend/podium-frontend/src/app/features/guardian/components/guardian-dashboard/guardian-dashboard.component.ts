import { Component, OnInit, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { GuardianService } from '../../services/guardian.service';
import { AuthService } from '../../../../features/auth/services/auth.service';
import { 
  GuardianDashboardDto, 
  GuardianLinkedStudentDto, 
  GuardianPendingApprovalDto 
} from '../../../../core/models/guardian.models';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { StudentStatusBadgeComponent } from '../../../../shared/components/student-status-badge/student-status-badge.component';
import { finalize } from 'rxjs';
import { KeyboardNavigationDirective, KeyboardShortcut } from '../../../../shared/directives/keyboard-navigation.directive';
import { BreadcrumbComponent, BreadcrumbItem } from '../../../../shared/components/breadcrumb/breadcrumb.component';

type Tab = 'activity' | 'offers' | 'requests' | 'profile' | 'videos';

@Component({
  selector: 'app-guardian-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    RouterLink,
    SkeletonLoaderComponent,
    StudentStatusBadgeComponent,
    BreadcrumbComponent
  ],
  templateUrl: './guardian-dashboard.component.html',
  styleUrls: ['./guardian-dashboard.component.scss'] // Ensure this file exists or remove if using inline styles
})
export class GuardianDashboardComponent implements OnInit {
  private guardianService = inject(GuardianService);
  private fb = inject(FormBuilder);
private router = inject(Router); 

  // --- State Signals ---
  dashboard = signal<GuardianDashboardDto | null>(null);
  selectedStudentId = signal<number | null>(null);
  activeTab = signal<Tab>('activity');
  isLoading = signal<boolean>(true);
  isDetailLoading = signal<boolean>(false); // Loading state for individual student data
  
  // --- Derived State (Computed Signals) ---
  students = computed(() => this.dashboard()?.linkedStudents || []);
  studentCount = computed(() => this.students().length);
  
  // The currently selected student object
  selectedStudent = computed(() => 
    this.students().find(s => s.studentId === this.selectedStudentId()) || null
  );

  // Selector Logic: 'tabs' for 2-5, 'dropdown' for 6+, 'none' for 0-1 (handled by auto-select)
  selectorType = computed(() => {
    const count = this.studentCount();
    if (count >= 6) return 'dropdown';
    if (count >= 2) return 'tabs';
    return 'none'; 
  });

  // Urgent Alerts (High Priority only)
  urgentAlerts = computed(() => 
    this.dashboard()?.priorityAlerts?.filter(a => a.severity === 'High') || []
  );

  // --- Tab Content Data Signals ---
  studentActivity = signal<any>(null);
  studentOffers = signal<any[]>([]);
  studentRequests = signal<any[]>([]);

  // --- Sorting State for Overview Table ---
  sortColumn = signal<keyof GuardianLinkedStudentDto | 'lastActivityDate'>('lastActivityDate');
  sortDirection = signal<'asc' | 'desc'>('desc');

  // --- Forms & Modals ---
  showApprovalModal = false;
  selectedApproval: GuardianPendingApprovalDto | null = null;

  isProcessingApproval = false;
  
  approvalForm: FormGroup = this.fb.group({
    approved: [true],
    notes: ['', Validators.maxLength(500)]
  });

  keyboardShortcuts: KeyboardShortcut[] = [];
  showKeyboardHelp = false;
  viewMode: 'list' | 'grid' = 'list';

    breadcrumbs = computed<BreadcrumbItem[]>(() => {
    const crumbs: BreadcrumbItem[] = [
      { label: 'Guardian Dashboard', url: '/guardian/dashboard' }
    ];

    const student = this.selectedStudent();
    if (student) {
      crumbs.push({
        label: student.studentName,
        url: `/guardian/student/${student.studentId}`
      });

      const tab = this.activeTab();
      if (tab !== 'activity') {
        crumbs.push({
          label: tab.charAt(0).toUpperCase() + tab.slice(1)
        });
      }
    } else {
      crumbs.push({ label: 'All Students' });
    }

    return crumbs;
  });

  constructor() {
    // Effect: Automatically fetch student details when a student is selected
    effect(() => {
      const studentId = this.selectedStudentId();
      if (studentId) {
        this.loadStudentDetails(studentId);
      }
    });
  }

  ngOnInit(): void {
    this.loadDashboard();
    this.guardianService.startSignalR();
    this.setupKeyboardShortcuts(); 
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.guardianService.getDashboard().subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.isLoading.set(false);
        
        // UX: If only 1 student, auto-select them. If 0, stay on overview (or link page).
        if (data.linkedStudents && data.linkedStudents.length === 1) {
          this.selectedStudentId.set(data.linkedStudents[0].studentId);
        }
      },
      error: (err) => {
        console.error('Dashboard load failed', err);
        this.isLoading.set(false);
      }
    });
  }

  // Fetch data specific to the selected student
loadStudentDetails(studentId: number): void {
    this.isDetailLoading.set(true);
    
    // Fixed: Added error handling for individual requests so one failure doesn't break the UI
    this.guardianService.getStudentActivity(studentId).subscribe({
      next: (data) => this.studentActivity.set(data),
      error: (err) => {
        console.error('Failed to load activity', err);
        this.studentActivity.set(null); // Fallback to empty array
      }
    });

    this.guardianService.getStudentOffers(studentId).subscribe({
      next: (data) => this.studentOffers.set(data),
      error: (err) => {
        console.error('Failed to load offers', err);
        this.studentOffers.set([]);
      }
    });

    this.guardianService.getContactRequests(studentId).pipe(
      finalize(() => this.isDetailLoading.set(false))
    ).subscribe({
      next: (data) => this.studentRequests.set(data),
      error: (err) => {
        console.error('Failed to load requests', err);
        this.studentRequests.set([]);
      }
    });
  }

  // --- Actions ---

  selectStudent(id: number | null): void {
    this.selectedStudentId.set(id);
    this.activeTab.set('activity'); // Reset tab to default
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  // --- Overview Table Sorting ---
  sortData(column: string): void {
    const currentDir = this.sortDirection();
    const currentCol = this.sortColumn();
    
    if (currentCol === column) {
      this.sortDirection.set(currentDir === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column as any);
      this.sortDirection.set('desc');
    }
  }

  // Computed property for the sorted list
  sortedStudents = computed(() => {
    const students = [...this.students()];
    const col = this.sortColumn();
    const dir = this.sortDirection() === 'asc' ? 1 : -1;

    return students.sort((a: any, b: any) => {
      const valA = a[col] || '';
      const valB = b[col] || '';
      
      if (valA < valB) return -1 * dir;
      if (valA > valB) return 1 * dir;
      return 0;
    });
  });


  // --- Approvals & Modals ---

  openApprovalModal(approval: GuardianPendingApprovalDto | any): void {
    this.selectedApproval = approval;
    this.approvalForm.reset({ approved: true, notes: '' });
    this.showApprovalModal = true;
  }

  closeApprovalModal(): void {
    this.showApprovalModal = false;
    this.selectedApproval = null;
  }

  submitApproval(): void {
    if (!this.selectedApproval) return;
    
    this.isProcessingApproval = true; // Set loading state
    const { approved, notes } = this.approvalForm.value;
    const response = approved ? 'Accepted' : 'Declined';

    this.guardianService.respondToScholarship(
      this.selectedApproval.offerId,
      response,
      notes
    ).pipe(
      finalize(() => this.isProcessingApproval = false) // Reset loading state
    ).subscribe({
      next: () => {
        this.closeApprovalModal();
        this.loadDashboard();
        if (this.selectedStudentId()) {
          this.loadStudentDetails(this.selectedStudentId()!);
        }
      },
      error: (err) => console.error('Response failed', err)
    });
  }

  unlinkStudent(studentId: number): void {
    if (confirm('Are you sure you want to unlink this student? You will lose all access to their data.')) {
      this.guardianService.unlinkStudent(studentId).subscribe(() => {
        this.loadDashboard(); // Reload to refresh list
        if (this.selectedStudentId() === studentId) {
          this.selectedStudentId.set(null); // Go back to overview
        }
      });
    }
  }

  // 4. Setup keyboard shortcuts
  private setupKeyboardShortcuts(): void {
    this.keyboardShortcuts = [
      {
        key: 'o',
        ctrl: true,
        description: 'Overview Mode',
        action: () => this.selectStudent(null)
      },
      {
        key: 'l',
        ctrl: true,
        description: 'Link New Student',
        action: () => this.router.navigate(['/guardian/link-student'])
      },
      {
        key: 'ArrowLeft',
        description: 'Previous Student',
        action: () => this.navigateStudent(-1)
      },
      {
        key: 'ArrowRight',
        description: 'Next Student',
        action: () => this.navigateStudent(1)
      },
      {
        key: 'Escape',
        description: 'Close Modal / Back to Overview',
        action: () => {
          if (this.showApprovalModal) {
            this.closeApprovalModal();
          } else if (this.selectedStudentId()) {
            this.selectStudent(null);
          }
        }
      },
      {
        key: '1',
        alt: true,
        description: 'Contact Requests Tab',
        action: () => {
          if (this.selectedStudentId()) {
            this.setTab('requests');
          }
        }
      },
      {
        key: '2',
        alt: true,
        description: 'Scholarship Offers Tab',
        action: () => {
          if (this.selectedStudentId()) {
            this.setTab('offers');
          }
        }
      },
      {
        key: '3',
        alt: true,
        description: 'Activity Timeline Tab',
        action: () => {
          if (this.selectedStudentId()) {
            this.setTab('activity');
          }
        }
      },
      {
        key: 'v',
        ctrl: true,
        description: 'Toggle View Mode (List/Grid)',
        action: () => {
          if (this.studentCount() >= 6) {
            this.viewMode = this.viewMode === 'list' ? 'grid' : 'list';
          }
        }
      },
      {
        key: '?',
        shift: true,
        description: 'Show Keyboard Shortcuts',
        action: () => {
          this.showKeyboardHelp = !this.showKeyboardHelp;
        }
      }
    ];
  }

  //Navigate between students with arrow keys
  private navigateStudent(direction: number): void {
    const studentsList = this.students();
    if (studentsList.length === 0) return;

    const currentIndex = studentsList.findIndex(s => s.studentId === this.selectedStudentId());
    
    if (currentIndex === -1) {
      // No student selected, select first
      this.selectStudent(studentsList[0].studentId);
    } else {
      const newIndex = (currentIndex + direction + studentsList.length) % studentsList.length;
      this.selectStudent(studentsList[newIndex].studentId);
    }
  }

  //  Toggle view mode
  toggleViewMode(): void {
    this.viewMode = this.viewMode === 'list' ? 'grid' : 'list';
  }

  // Toggle keyboard help
  toggleKeyboardHelp(): void {
    this.showKeyboardHelp = !this.showKeyboardHelp;
  }
}