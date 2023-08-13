import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, OnDestroy, Renderer2, ViewChild } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Subscription } from "rxjs";

import { LoggerService } from "../logger.service";
import { IMessage } from "../IMessage";

@Component({
  selector: "app-log-viewer",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./log-viewer.component.html",
  styleUrls: ["./log-viewer.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LogViewerComponent implements AfterViewInit, OnDestroy {

  @ViewChild("messages", { read: ElementRef })
  private _messages: ElementRef<HTMLElement> | undefined;
  private _subs: Subscription[] = [];

  public constructor(private readonly _loggerService: LoggerService, private _renderer: Renderer2) {
  }

  public ngAfterViewInit(): void {
    this._subs.push(this._loggerService.stream$.subscribe((a: IMessage) => this.onMessage(a)));
    this._subs.push(this._loggerService.onClear.subscribe(() => Array.from(this._messages!.nativeElement.children).forEach(c => c.remove())));
  }

  public ngOnDestroy(): void {
    for (const sub of this._subs) {
      sub.unsubscribe();
    }
  }

  private onMessage(msg: IMessage): void {
    const element = this._renderer.createElement("div") as HTMLElement;
    element.classList.add(msg.type + "Row");
    const text = this._renderer.createText(msg.content["entry"] ?? msg.content as string);
    this._renderer.appendChild(element, text);
    this._renderer.appendChild(this._messages!.nativeElement, element);
    if (this.getSelectedText() == "") {
      element.scrollIntoView();
    }
  }

  private getSelectedText() {
    return window.getSelection()?.toString() ?? "";
  }
}
