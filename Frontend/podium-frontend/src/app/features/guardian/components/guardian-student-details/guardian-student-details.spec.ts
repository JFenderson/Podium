import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GuardianStudentDetailsComponent } from './guardian-student-details.component';

describe('GuardianStudentDetailsComponent', () => {
  let component: GuardianStudentDetailsComponent;
  let fixture: ComponentFixture<GuardianStudentDetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuardianStudentDetailsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GuardianStudentDetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
