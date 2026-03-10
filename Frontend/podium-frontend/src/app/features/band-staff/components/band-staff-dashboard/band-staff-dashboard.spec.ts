import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BandStaffDashboardComponent } from './band-staff-dashboard.component';

describe('BandStaffDashboardComponent', () => {
  let component: BandStaffDashboardComponent;
  let fixture: ComponentFixture<BandStaffDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BandStaffDashboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BandStaffDashboardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
