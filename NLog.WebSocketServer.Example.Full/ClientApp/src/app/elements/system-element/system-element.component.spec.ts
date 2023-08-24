import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SystemElementComponent } from './system-element.component';

describe('SystemElementComponent', () => {
  let component: SystemElementComponent;
  let fixture: ComponentFixture<SystemElementComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SystemElementComponent]
    });
    fixture = TestBed.createComponent(SystemElementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
