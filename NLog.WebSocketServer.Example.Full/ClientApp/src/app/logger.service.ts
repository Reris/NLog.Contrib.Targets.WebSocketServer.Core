import { EventEmitter, Injectable } from "@angular/core";
import { BehaviorSubject, catchError, map, of, pairwise, ReplaySubject, shareReplay, switchMap } from "rxjs";

import { environment } from "../environments/environment";
import { IMessage } from "./IMessage";
import { LogSettings } from "./LogSettings";

@Injectable({
  providedIn: "root"
})
export class LoggerService {
  public onClear = new EventEmitter();
  private _settings$ = new BehaviorSubject<LogSettings>(new LogSettings());
  public settings$ = this._settings$.asObservable();
  private _server$ = new ReplaySubject<string | null>(2);
  private _websocket$ =
    this._server$.pipe(map((a) => a != null ? new WebSocket(a) : null), pairwise(), map(([a, b]) => this.disconnect(a, b)), shareReplay(1));
  private _currentStream$ = this._websocket$.pipe(map(a => this.connect(a)));
  public stream$ = this._currentStream$.pipe(switchMap(a => a), catchError(e => of<IMessage>({ type: "system", content: e })));

  constructor() {
    this._server$.next(null);
    this._server$.next(this.determineServer());
  }

  public clear() {
    this.onClear.next(undefined);
  }

  private determineServer() {
    if (environment.production) {
      return `ws://${window.location.host}/wslogger/listen`;
    }

    return "ws://localhost:5095/wslogger/listen";
  }

  private disconnect(previous: WebSocket | null, next: WebSocket | null): WebSocket | null {
    previous?.close();
    return next;
  }

  private connect(start: WebSocket | null): ReplaySubject<IMessage> {
    const s$ = new ReplaySubject<IMessage>();

    if (start != null) {
      let wasConnected = start.readyState === start.OPEN;
      if (!wasConnected) {
        s$.next({ type: "system", content: `connecting to '${start.url}'` });
      }

      start.addEventListener("open", () => {
        wasConnected = true;
        s$.next({ type: "system", content: `connected to '${start.url}'` })
      });
      start.addEventListener("close", e => {
        if (wasConnected) {
          s$.next({ type: "system", content: `closed '${start.url}', ${e.code}:${e.reason}` });
        } else {
          s$.next({ type: "system", content: `could not connect to '${start.url}'` });
        }
      });
      start.addEventListener("message", e => {
        if (e.data != null) {
          s$.next({ type: "log", content: JSON.parse(e.data) });
        }
      });
    } else {
      s$.next({ type: "system", content: "Not connected" });
    }

    return s$;
  }
}
