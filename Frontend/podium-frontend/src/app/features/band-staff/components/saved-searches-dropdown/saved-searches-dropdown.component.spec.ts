import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SavedSearchesDropdownComponent } from './saved-searches-dropdown.component';

describe('SavedSearchesDropdownComponent', () => {
  let component: SavedSearchesDropdownComponent;
  let fixture: ComponentFixture<SavedSearchesDropdownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SavedSearchesDropdownComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SavedSearchesDropdownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
