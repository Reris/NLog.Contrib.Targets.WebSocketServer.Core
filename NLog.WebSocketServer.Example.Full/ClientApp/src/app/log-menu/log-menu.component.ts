import { ChangeDetectionStrategy, Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import { BehaviorSubject } from "rxjs";

import { LoggerService } from "../logger.service";
import { LogSettingsComponent } from "../log-settings/log-settings.component";

@Component({
  selector: "app-log-menu",
  standalone: true,
  imports: [CommonModule, LogSettingsComponent],
  templateUrl: "./log-menu.component.html",
  styleUrls: ["./log-menu.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogMenuComponent {

  public readonly settingsOpen$ = new BehaviorSubject<boolean>(false);

  public constructor(private readonly _loggerService: LoggerService) {
  }

  public clear() {
    this._loggerService.clear();
  }

  public openSettings() {
    this.settingsOpen$.next(!this.settingsOpen$.value)
  }
}
