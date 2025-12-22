import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GuardianLayout } from './guardian-layout';

describe('GuardianLayout', () => {
  let component: GuardianLayout;
  let fixture: ComponentFixture<GuardianLayout>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuardianLayout]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GuardianLayout);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
