import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { GuardianService } from '../../services/guardian.service';
import { 
  StudentProfileViewDto, 
  GuardianScholarshipDto, 
  StudentActivityReportDto 
} from '../../../../core/models/guardian.models';
import { forkJoin } from 'rxjs';
import { StudentStatusBadgeComponent } from '../../../../shared/components/student-status-badge/student-status-badge.component';

@Component({
  selector: 'app-guardian-student-details',
  standalone: true,
  imports: [CommonModule, StudentStatusBadgeComponent],
  templateUrl: './guardian-student-details.component.html'
})
export class GuardianStudentDetailsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private guardianService = inject(GuardianService);

  studentId: number = 0;
  isLoading = true;
  activeTab: 'overview' | 'scholarships' | 'events' = 'overview';

  // Data
  profile: StudentProfileViewDto | null = null;
  scholarships: GuardianScholarshipDto[] = [];
  activity: StudentActivityReportDto | null = null;

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.studentId = +params['id'];
      if (this.studentId) this.loadStudentData();
    });
  }

  loadStudentData() {
    this.isLoading = true;
    
    forkJoin({
      profile: this.guardianService.getStudentProfile(this.studentId),
      scholarships: this.guardianService.getStudentOffers(this.studentId),
      activity: this.guardianService.getStudentActivity(this.studentId)
    }).subscribe({
      next: (data) => {
        this.profile = data.profile;
        this.scholarships = data.scholarships;
        this.activity = data.activity;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading student details', err);
        this.isLoading = false;
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'accepted': return 'bg-green-100 text-green-800';
      case 'sent': 
      case 'pending': return 'bg-blue-100 text-blue-800';
      case 'declined': return 'bg-red-100 text-red-800';
      case 'expired': return 'bg-gray-100 text-gray-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  }
}