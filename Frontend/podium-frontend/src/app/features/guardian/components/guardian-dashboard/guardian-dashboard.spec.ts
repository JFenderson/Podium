import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GuardianDashboard } from './guardian-dashboard';

describe('GuardianDashboard', () => {
  let component: GuardianDashboard;
  let fixture: ComponentFixture<GuardianDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuardianDashboard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GuardianDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
