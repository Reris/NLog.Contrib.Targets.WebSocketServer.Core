import { ChangeDetectionStrategy, Component } from "@angular/core";
import { CommonModule } from "@angular/common";

import { LoggerService } from "../log-viewer/logger.service";

@Component({
  selector: "app-log-menu",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./log-menu.component.html",
  styleUrls: ["./log-menu.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogMenuComponent {
  public constructor(private readonly _loggerService: LoggerService) {
  }

  public clear() {
    this._loggerService.clear();
  }
}
