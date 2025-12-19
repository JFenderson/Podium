import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ScholarshipCreate } from './scholarship-create';

describe('ScholarshipCreate', () => {
  let component: ScholarshipCreate;
  let fixture: ComponentFixture<ScholarshipCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScholarshipCreate]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ScholarshipCreate);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
