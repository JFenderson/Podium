import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LinkStudent } from './link-student.component';

describe('LinkStudent', () => {
  let component: LinkStudent;
  let fixture: ComponentFixture<LinkStudent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LinkStudent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LinkStudent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
