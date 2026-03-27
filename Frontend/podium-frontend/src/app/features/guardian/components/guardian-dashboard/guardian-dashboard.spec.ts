import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GuardianDashboardComponent } from './guardian-dashboard.component';

describe('GuardianDashboardComponent', () => {
  let component: GuardianDashboardComponent;
  let fixture: ComponentFixture<GuardianDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuardianDashboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GuardianDashboardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
