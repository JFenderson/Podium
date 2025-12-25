import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StudentStatusBadgeComponent } from './student-status-badge.component';

describe('StudentStatusBadgeComponent', () => {
  let component: StudentStatusBadgeComponent;
  let fixture: ComponentFixture<StudentStatusBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StudentStatusBadgeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StudentStatusBadgeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
