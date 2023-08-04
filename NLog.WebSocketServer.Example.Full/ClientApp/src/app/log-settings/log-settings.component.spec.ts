import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LogSettingsComponent } from './log-settings.component';

describe('LogSettingsComponent', () => {
  let component: LogSettingsComponent;
  let fixture: ComponentFixture<LogSettingsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LogSettingsComponent]
    });
    fixture = TestBed.createComponent(LogSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
