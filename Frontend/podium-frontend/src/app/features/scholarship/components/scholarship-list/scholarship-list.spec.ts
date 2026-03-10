import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ScholarshipListComponent } from './scholarship-list.component';

describe('ScholarshipListComponent', () => {
  let component: ScholarshipListComponent;
  let fixture: ComponentFixture<ScholarshipListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScholarshipListComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ScholarshipListComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
