import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManageSavedSearchesComponent } from './manage-saved-searches.component';

describe('ManageSavedSearchesComponent', () => {
  let component: ManageSavedSearchesComponent;
  let fixture: ComponentFixture<ManageSavedSearchesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManageSavedSearchesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ManageSavedSearchesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
