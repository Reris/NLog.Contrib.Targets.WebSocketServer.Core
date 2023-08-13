import { ChangeDetectionStrategy, Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule } from "@angular/forms";

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
  protected readonly FormGroup = FormGroup;
  private _subs: Subscription[] = [];

  public constructor(private readonly _loggerService: LoggerService, fb: FormBuilder) {

    const defaultSettings = new LogSettings();
    this.settingsForm = fb.group({
      maxRows: [defaultSettings.maxRows],
      colors: fb.group({
        system: [defaultSettings.colors.system],
        trace: [defaultSettings.colors.trace],
        debug: [defaultSettings.colors.debug],
        info: [defaultSettings.colors.info],
        warn: [defaultSettings.colors.warn],
        error: [defaultSettings.colors.error],
        fatal: [defaultSettings.colors.fatal],
      }),
      sources: fb.array([fb.group({
        source: [defaultSettings.sources[0].source],
        inputFormat: [defaultSettings.sources[0].inputFormat],
        csvFormat: [defaultSettings.sources[0].csvFormat],
      })])
    });

    this._subs.push(this._loggerService.settings$.subscribe(a => this.settingsForm.patchValue(a)));
  }

  public get sources(): FormGroup[] {
    return (this.settingsForm.controls["sources"] as FormArray).controls as FormGroup[];
  }

  public ngOnDestroy(): void {
    for (const sub of this._subs) {
      sub.unsubscribe();
    }
  }

  public apply() {
    let settings = new LogSettings();
    Object.assign(settings, this.settingsForm.value);
    this._loggerService.updateSettings(settings);
  }
}
