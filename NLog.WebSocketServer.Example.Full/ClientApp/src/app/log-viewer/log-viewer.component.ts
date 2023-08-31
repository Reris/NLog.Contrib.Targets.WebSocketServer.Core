import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  Injector,
  OnDestroy,
  Type,
  ViewChild,
  ViewContainerRef
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { combineLatest, firstValueFrom, map, Subscription } from "rxjs";

import { LoggerService } from "../logger.service";
import { LogEvent } from "../LogEvent";
import { SystemEvent } from "../SystemEvent";
import { LogElementComponent } from "../elements/log-element/log-element.component";
import { SystemElementComponent } from "../elements/system-element/system-element.component";
import { LogLevel } from "../LogLevel";

@Component({
  selector: "app-log-viewer",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./log-viewer.component.html",
  styleUrls: ["./log-viewer.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogViewerComponent implements AfterViewInit, OnDestroy {
  private _stylesColor$ = this._loggerService.settings$.pipe(map(a => {
    const styleObject: Record<string, string> = {};
    styleObject["--log-color-system"] = a.colors.system;
    styleObject["--log-color-trace"] = a.colors.trace;
    styleObject["--log-color-debug"] = a.colors.debug;
    styleObject["--log-color-info"] = a.colors.info;
    styleObject["--log-color-warn"] = a.colors.warn;
    styleObject["--log-color-error"] = a.colors.error;
    styleObject["--log-color-fatal"] = a.colors.fatal;
    return styleObject;
  }));

  private _stylesVisible$ = this._loggerService.minLevel$.pipe(map(a => {
    const styleObject: Record<string, string> = {};
    console.log(a);
    if (a > LogLevel.Trace) {
      styleObject["--log-visible-trace"] = "none";
    }

    if (a > LogLevel.Debug) {
      styleObject["--log-visible-debug"] = "none";
    }

    if (a > LogLevel.Info) {
      styleObject["--log-visible-info"] = "none";
    }

    if (a > LogLevel.Warn) {
      styleObject["--log-visible-warn"] = "none";
    }

    if (a > LogLevel.Error) {
      styleObject["--log-visible-error"] = "none";
    }

    if (a > LogLevel.Fatal) {
      styleObject["--log-visible-fatal"] = "none";
    }

    return styleObject;
  }));

  public styles$ = combineLatest([this._stylesColor$, this._stylesVisible$])
    .pipe(map((all) => {
      const result: Record<string, string> = {};
      for (const a of all) {
        Object.assign(result, a);
      }
      return result;
    }));

  @ViewChild("messages", { read: ViewContainerRef })
  private _messages: ViewContainerRef | undefined;
  private _subs: Subscription[] = [];

  public constructor(private readonly _loggerService: LoggerService) {
  }

  public ngAfterViewInit(): void {
    this._subs.push(this._loggerService.stream$.subscribe(a => this.onMessage(a)));
    this._subs.push(this._loggerService.onClear.subscribe(() => this._messages!.clear()));
  }

  public ngOnDestroy(): void {
    for (const sub of this._subs) {
      sub.unsubscribe();
    }
  }

  private onMessage(logEvent: LogEvent | SystemEvent): void {
    let componentType: Type<unknown>;
    switch (logEvent.type) {
      case "system":
        componentType = SystemElementComponent;
        break;
      case "log":
        componentType = LogElementComponent;
        break;
    }

    // Very static values, but lots of them. Keep refreshes away and inject the value to have it fully ready at start
    const injector = Injector.create({ providers: [{ provide: "logEvent", useValue: logEvent }] });
    const element = this._messages!.createComponent(componentType, { injector });
    this.applyMaxRows().then(/*Fire & Forget*/);
    element.hostView.detectChanges();
    if (this.getSelectedText() == "") {
      element.location.nativeElement.scrollIntoView();
    }
  }

  private getSelectedText() {
    return window.getSelection()?.toString() ?? "";
  }

  private async applyMaxRows() {
    const maxRows = (await firstValueFrom(this._loggerService.settings$)).maxRows;
    while (maxRows > 0 && this._messages!.length > maxRows) {
      this._messages!.remove(0);
    }

  }
}
