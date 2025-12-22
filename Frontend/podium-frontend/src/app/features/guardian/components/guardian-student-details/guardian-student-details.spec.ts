import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GuardianStudentDetails } from './guardian-student-details.component';

describe('GuardianStudentDetails', () => {
  let component: GuardianStudentDetails;
  let fixture: ComponentFixture<GuardianStudentDetails>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuardianStudentDetails]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GuardianStudentDetails);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
