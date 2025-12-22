import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Guardian } from './guardian-profile.component';

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
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
