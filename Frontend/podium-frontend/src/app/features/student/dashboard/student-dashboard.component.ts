import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StudentService } from '../../../core/services/student.service';
import { StudentDetailsDto } from '../../../core/models/student.models';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './student-dashboard.component.html'
})
export class StudentDashboardComponent implements OnInit {
  private studentService = inject(StudentService);
  profile: StudentDetailsDto | null = null;

  ngOnInit() {
    this.studentService.getMyProfile().subscribe(data => this.profile = data);
  }
}