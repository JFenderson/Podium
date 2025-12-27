import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { StudentService } from '../../services/student.service';
import { StudentDashboardDto, StudentDetailsDto, UpdateStudentDto } from '../../../../core/models/student.models';
import { Clipboard } from '@angular/cdk/clipboard'; // Optional: for 'Copy Code' button
import { NotificationCardComponent } from '../../../../shared/components/notifications/notification-card.component';
import { NotificationService } from '../../../../core/services/notification.service';
import { NotificationDto } from '../../../../core/models/notification.models';
import { finalize } from 'rxjs/operators';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { KeyboardNavigationDirective, KeyboardShortcut } from '../../../../shared/directives/keyboard-navigation.directive';


@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule], // Add MatIconModule if using Angular Material
  templateUrl: './student-dashboard.component.html',
  styleUrls: ['./student-dashboard.component.scss']
})
export class StudentDashboardComponent implements OnInit {
  private studentService = inject(StudentService);
  private clipboard = inject(Clipboard);
  public notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
keyboardShortcuts: KeyboardShortcut[] = [];
  showKeyboardHelp = false;

  // Data
  dashboard: StudentDashboardDto | null = null;
  profile: StudentDetailsDto | null = null;
  
  // UI States
  isLoading = true;
  copySuccess = false;
  profileProgress = 0;
  
  // Wizard State
  showWizard = false;
  wizardStep = 1;
  isSavingWizard = false;
  videoCount = 0;
  
  // Forms
basicsForm!: FormGroup;
  musicForm!: FormGroup;
  selectedVideoFile: File | null = null;

  ngOnInit(): void {
    this.loadDashboard();
    this.loadData();
    this.initForms();
    this.setupKeyboardShortcuts();
    this.notificationService.getRecentNotifications().subscribe();
    this.notificationService.getUnreadCount().subscribe();
  }

  //Profile Setup Wizard Methods
  private initForms(): void {
    this.basicsForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', Validators.required],
      phoneNumber: ['', Validators.required],
      dateOfBirth: [null]
    });

    this.musicForm = this.fb.group({
      primaryInstrument: ['', Validators.required],
      yearsExperience: [null, [Validators.min(0)]],
      skillLevel: [''],
      bioDescription: ['', [Validators.required, Validators.minLength(20)]]
    });
  }

  loadData(): void {
    this.isLoading = true;

    forkJoin({
      dashboard: this.studentService.getDashboard(),
      profile: this.studentService.getMyProfile()
    }).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: ({ dashboard, profile }) => {
        this.dashboard = dashboard;
        this.profile = profile;
        
        // Calculate video count based on profile data
        this.videoCount = this.profile.videoUrl ? 1 : 0; 

        this.calculateProgress();
        this.checkFirstTimeUser();
      },
      error: (err) => console.error('Failed to load dashboard data', err)
    });
  }

  calculateProgress(): void {
    if (!this.profile) return;

    const criticalFields = [
      this.profile.firstName,
      this.profile.lastName,
      this.profile.phoneNumber,
      this.profile.city,
      this.profile.state,
      this.profile.primaryInstrument,
      this.profile.bio, 
    ];

    const filled = criticalFields.filter(f => f && f.toString().length > 0).length;
    
    let score = (filled / criticalFields.length) * 80;
    if (this.profile.videoUrl) score += 20;

    this.profileProgress = Math.round(score);
  }

  checkFirstTimeUser(): void {
    const forceSetup = this.route.snapshot.queryParams['setup'] === 'true';
    
    if (forceSetup || (this.profile && this.profile.accountStatus === 'Incomplete') || this.profileProgress < 20) {
      this.openWizard();
    }
  }
  // --- Wizard Logic ---
openWizard(): void {
    this.showWizard = true;
    this.wizardStep = 1;
    
    if (this.profile) {
      this.basicsForm.patchValue({
        firstName: this.profile.firstName,
        lastName: this.profile.lastName,
        city: this.profile.city,
        state: this.profile.state,
        phoneNumber: this.profile.phoneNumber,
        dateOfBirth: this.profile.dateOfBirth
      });
      
      this.musicForm.patchValue({
        primaryInstrument: this.profile.primaryInstrument,
        yearsExperience: this.profile.yearsOfExperience,
        skillLevel: this.profile.skillLevel,
        bioDescription: this.profile.bio 
      });
    }
  }

  closeWizard(): void {
    this.showWizard = false;
    this.router.navigate([], { queryParams: { setup: null }, queryParamsHandling: 'merge' });
    this.loadData();
  }

  nextStep(): void {
    if (this.wizardStep === 2) this.saveBasics();
    else if (this.wizardStep === 3) this.saveMusic();
    else this.wizardStep++;
  }

  prevStep(): void {
    if (this.wizardStep > 1) this.wizardStep--;
  }

  saveBasics(): void {
    if (this.basicsForm.invalid) return;
    this.isSavingWizard = true;

    const dto: UpdateStudentDto = this.basicsForm.value;
    // Non-null assertion (!) for this.profile since we check it in loadData
    this.studentService.updateStudent(this.profile!.studentId, dto).subscribe({
      next: () => {
        this.isSavingWizard = false;
        this.wizardStep = 3;
      },
      error: () => this.isSavingWizard = false
    });
  }

  saveMusic(): void {
    if (this.musicForm.invalid) return;
    this.isSavingWizard = true;

    const dto: UpdateStudentDto = this.musicForm.value;
    this.studentService.updateStudent(this.profile!.studentId, dto).subscribe({
      next: () => {
        this.isSavingWizard = false;
        this.wizardStep = 4;
      },
      error: () => this.isSavingWizard = false
    });
  }

  onFileSelected(event: any): void {
    if (event.target.files && event.target.files.length > 0) {
      this.selectedVideoFile = event.target.files[0];
    }
  }

  uploadFirstVideo(): void {
    if (!this.selectedVideoFile || !this.profile) return;
    this.isSavingWizard = true;

    const formData = new FormData();
    formData.append('file', this.selectedVideoFile);
    formData.append('title', 'My First Audition');
    // Safe navigation for get()
    formData.append('instrument', this.musicForm.get('primaryInstrument')?.value || this.profile.primaryInstrument || 'Unknown');

    this.studentService.uploadVideo(this.profile.studentId, formData).subscribe({
      next: () => {
        this.isSavingWizard = false;
        this.wizardStep = 5;
      },
      error: (err) => {
        console.error('Video upload failed', err);
        this.isSavingWizard = false;
      }
    });
  }

  skipVideo(): void {
    this.wizardStep = 5;
  }

  // --- Utilities ---

  copyInviteCode(code: string): void {
    if (this.clipboard.copy(code)) {
      this.copySuccess = true;
      setTimeout(() => this.copySuccess = false, 2000);
    }
  }

  get isProfileComplete(): boolean {
    return this.profileProgress === 100;
  }


//Dashboard Methods
  loadDashboard(): void {
    this.studentService.getDashboard().subscribe({
      next: (data: StudentDashboardDto) => {
        this.dashboard = data;
        this.isLoading = false;
      },
      error: (err: any) => {
        console.error('Failed to load dashboard', err);
        this.isLoading = false;
      }
    });
  }


  onMarkAsRead(notification: NotificationDto): void {
    this.notificationService.markAsRead(notification.notificationId).subscribe({
      next: () => console.log('Marked as read'),
      error: (err) => console.error('Error marking as read', err)
    });
  }

  onDismiss(notification: NotificationDto): void {
    this.notificationService.deleteNotification(notification.notificationId).subscribe({
      next: () => console.log('Dismissed notification'),
      error: (err) => console.error('Error dismissing notification', err)
    });
  }

  private setupKeyboardShortcuts(): void {
    this.keyboardShortcuts = [
      {
        key: 'p',
        ctrl: true,
        description: 'Complete Profile',
        action: () => {
          if (!this.isProfileComplete) {
            this.openWizard();
          }
        }
      },
      {
        key: 'u',
        ctrl: true,
        description: 'Upload Video',
        action: () => {
          this.router.navigate(['/student/videos/upload']);
        }
      },
      {
        key: 'b',
        ctrl: true,
        description: 'Browse Bands',
        action: () => {
          this.router.navigate(['/bands']);
        }
      },
      {
        key: 'Escape',
        description: 'Close Modal',
        action: () => {
          if (this.showWizard) {
            this.closeWizard();
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

    toggleKeyboardHelp(): void {
    this.showKeyboardHelp = !this.showKeyboardHelp;
  }
}