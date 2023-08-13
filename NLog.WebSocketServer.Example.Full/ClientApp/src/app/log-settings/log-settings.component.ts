import { ChangeDetectionStrategy, Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule } from "@angular/forms";

import { LoggerService } from "../logger.service";
import { Subscription } from "rxjs";
import { LogSettings } from "../LogSettings";

@Component({
  selector: 'app-log-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './log-settings.component.html',
  styleUrls: ['./log-settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogSettingsComponent implements OnDestroy {

  public settingsForm: FormGroup;
  private _subs: Subscription[] = [];

  public constructor(private readonly _loggerService: LoggerService) {

    const defaultSettings = new LogSettings();
    this.settingsForm = new FormGroup({
      maxRows: new FormControl(defaultSettings.maxRows),
      source: new FormGroup({
        source: new FormControl(defaultSettings.sources[0].source),
        inputFormat: new FormControl(defaultSettings.sources[0].inputFormat),
        csvFormat: new FormControl(defaultSettings.sources[0].csvFormat),
      }),
      colors: new FormGroup({
        system: new FormControl(defaultSettings.colors.system),
        trace: new FormControl(defaultSettings.colors.trace),
        debug: new FormControl(defaultSettings.colors.debug),
        info: new FormControl(defaultSettings.colors.info),
        warn: new FormControl(defaultSettings.colors.warn),
        error: new FormControl(defaultSettings.colors.error),
        fatal: new FormControl(defaultSettings.colors.fatal),
      })
    });

    this._subs.push(this._loggerService.settings$.subscribe(a => this.settingsForm.patchValue(a)));
  }

  public ngOnDestroy(): void {
    for (const sub of this._subs) {
      sub.unsubscribe();
    }
  }

  public apply() {
    console.log(this.settingsForm.value);
    //this._loggerService
  }
}
