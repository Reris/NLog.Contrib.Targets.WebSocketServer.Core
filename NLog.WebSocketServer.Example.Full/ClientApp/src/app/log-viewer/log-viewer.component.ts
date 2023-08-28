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
import { map, Subscription } from "rxjs";

import { LoggerService } from "../logger.service";
import { LogEvent } from "../LogEvent";
import { SystemEvent } from "../SystemEvent";
import { LogElementComponent } from "../elements/log-element/log-element.component";
import { SystemElementComponent } from "../elements/system-element/system-element.component";

@Component({
  selector: "app-log-viewer",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./log-viewer.component.html",
  styleUrls: ["./log-viewer.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogViewerComponent implements AfterViewInit, OnDestroy {
  public styles$ = this._loggerService.settings$.pipe(map(a => {
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
    element.hostView.detectChanges();    
    if (this.getSelectedText() == "") {
      element.location.nativeElement.scrollIntoView();
    }
  }

  private getSelectedText() {
    return window.getSelection()?.toString() ?? "";
  }
}
