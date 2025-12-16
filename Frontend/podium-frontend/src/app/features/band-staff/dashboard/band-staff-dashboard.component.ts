import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BandStaffService } from '../../../core/services/band-staff.service';
import { BandStaffDto } from '../../../core/models/band-staff.models';

@Component({
  selector: 'app-staff-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './band-staff-dashboard.component.html'
})
export class BandStaffDashboardComponent implements OnInit {
  private staffService = inject(BandStaffService);
  staff: BandStaffDto | null = null;

  ngOnInit() {
    this.staffService.getMyInfo().subscribe(data => this.staff = data);
  }
}