import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ScholarshipList } from './scholarship-list';

describe('ScholarshipList', () => {
  let component: ScholarshipList;
  let fixture: ComponentFixture<ScholarshipList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScholarshipList]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ScholarshipList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
