import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BandStaffDashboard } from './band-staff-dashboard';

describe('BandStaffDashboard', () => {
  let component: BandStaffDashboard;
  let fixture: ComponentFixture<BandStaffDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BandStaffDashboard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BandStaffDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
