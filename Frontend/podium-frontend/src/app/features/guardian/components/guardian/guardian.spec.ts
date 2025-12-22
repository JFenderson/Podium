import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Guardian } from './guardian';

describe('Guardian', () => {
  let component: Guardian;
  let fixture: ComponentFixture<Guardian>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Guardian]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Guardian);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
