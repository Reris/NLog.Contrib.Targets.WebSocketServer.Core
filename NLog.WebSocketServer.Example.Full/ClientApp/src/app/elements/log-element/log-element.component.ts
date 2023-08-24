import { ChangeDetectionStrategy, Component, Inject } from "@angular/core";
import { CommonModule } from "@angular/common";

import { ILogEvent } from "../../ILogEvent";

@Component({
  selector: "app-log-element",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./log-element.component.html",
  styleUrls: ["./log-element.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogElementComponent {
  public constructor(@Inject("logEvent") public readonly logEvent: ILogEvent) {
  }
}
