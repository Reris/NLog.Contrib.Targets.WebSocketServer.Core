import { ChangeDetectionStrategy, Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import { BehaviorSubject } from "rxjs";

import { LoggerService } from "../logger.service";
import { LogSettingsComponent } from "../log-settings/log-settings.component";
import { LogLevel } from "../LogLevel";
import { FormsModule } from "@angular/forms";

@Component({
  selector: "app-log-menu",
  standalone: true,
  imports: [CommonModule, LogSettingsComponent, FormsModule],
  templateUrl: "./log-menu.component.html",
  styleUrls: ["./log-menu.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogMenuComponent {

  public readonly settingsOpen$ = new BehaviorSubject<boolean>(false);
  public readonly paused$ = this._loggerService.paused$;
  public readonly minLevel$ = this._loggerService.minLevel$;
  protected readonly LogLevel = LogLevel;

  public constructor(private readonly _loggerService: LoggerService) {
  }

  public setMinLevel(level: LogLevel) {
    this._loggerService.setMinLevel(level);
  }

  public clear() {
    this._loggerService.clear();
  }

  public openSettings() {
    this.settingsOpen$.next(!this.settingsOpen$.value)
  }

  public pause() {
    this._loggerService.pause();
  }
}
