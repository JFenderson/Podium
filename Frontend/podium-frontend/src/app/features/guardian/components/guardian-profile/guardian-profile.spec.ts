import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GuardianProfileComponent } from './guardian-profile.component';

describe('GuardianProfileComponent', () => {
  let component: GuardianProfileComponent;
  let fixture: ComponentFixture<GuardianProfileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuardianProfileComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GuardianProfileComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
