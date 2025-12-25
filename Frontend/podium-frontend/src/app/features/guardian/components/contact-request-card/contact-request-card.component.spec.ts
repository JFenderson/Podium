import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ContactRequestCardComponent } from './contact-request-card.component';

describe('ContactRequestCardComponent', () => {
  let component: ContactRequestCardComponent;
  let fixture: ComponentFixture<ContactRequestCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContactRequestCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ContactRequestCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
