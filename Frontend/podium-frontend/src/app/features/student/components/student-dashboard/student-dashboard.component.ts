import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StudentService } from '../../services/student.service';
import { StudentDashboardDto } from '../../../../core/models/student.models';
import { Clipboard } from '@angular/cdk/clipboard'; // Optional: for 'Copy Code' button

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink], // Add MatIconModule if using Angular Material
  templateUrl: './student-dashboard.component.html',
  styleUrls: ['./student-dashboard.component.scss']
})
export class StudentDashboardComponent implements OnInit {
  private studentService = inject(StudentService);
  private clipboard = inject(Clipboard); // Optional

  dashboard: StudentDashboardDto | null = null;
  isLoading = true;
  copySuccess = false;

  ngOnInit(): void {
    this.loadDashboard();
  }

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

  copyInviteCode(code: string): void {
    const success = this.clipboard.copy(code);
    if (success) {
      this.copySuccess = true;
      setTimeout(() => this.copySuccess = false, 2000);
    }
  }
}