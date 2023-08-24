import { ComponentFixture, TestBed } from "@angular/core/testing";
import { LogElementComponent } from "./log-element.component";

describe("LogElementComponent", () => {
  let component: LogElementComponent;
  let fixture: ComponentFixture<LogElementComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LogElementComponent]
    });
    fixture = TestBed.createComponent(LogElementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
