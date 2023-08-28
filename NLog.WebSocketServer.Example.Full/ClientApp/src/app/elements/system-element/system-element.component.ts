import { ChangeDetectionStrategy, Component, Inject } from "@angular/core";
import { CommonModule } from "@angular/common";

import { SystemEvent } from "../../SystemEvent";

@Component({
  selector: "app-system-element",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./system-element.component.html",
  styleUrls: ["./system-element.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SystemElementComponent {
  public constructor(@Inject("logEvent") public readonly logEvent: SystemEvent) {
  }
}
