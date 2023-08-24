import { EventEmitter, Injectable } from "@angular/core";
import { BehaviorSubject, catchError, map, of, pairwise, ReplaySubject, shareReplay, switchMap } from "rxjs";

import { IMessage } from "./IMessage";
import { LogSettings } from "./LogSettings";
import * as Papa from "papaparse";
import { ILogEvent } from "./ILogEvent";
import { ILogEntry } from "./ILogEntry";
import { ISystemEvent } from "./ISystemEvent";

@Injectable({
  providedIn: "root"
})
export class LoggerService {
  public onClear = new EventEmitter();
  private _settings$ = new BehaviorSubject<LogSettings>(new LogSettings());
  public settings$ = this._settings$.asObservable();
  private _server$ = new ReplaySubject<string | null>(2);
  private _websocket$ = this._server$.pipe(
    map((a) => a != null ? new WebSocket(a) : null),
    pairwise(),
    map(([a, b]) => this.disconnect(a, b)),
    shareReplay(1));
  private _currentStream$ = this._websocket$.pipe(map(a => this.connect(a)));
  public stream$ = this._currentStream$.pipe(
    switchMap(a => a),
    catchError(e => of<ILogEvent | ISystemEvent>({ type: "system", content: e.toString() })));

  constructor() {
    this._server$.next(null);
    this.settings$.subscribe(a => {
      this._parser = this.getParser(a);
      this._server$.next(a.sources[0].source);
    });
  }

  public clear() {
    this.onClear.next(undefined);
  }

  public updateSettings(settings: LogSettings) {
    this._settings$.next(settings);
  }

  private _parser: (e: MessageEvent) => (ILogEvent | ISystemEvent | null) = () => ({
    type: "system",
    content: "No parser defined"
  });

  private disconnect(previous: WebSocket | null, next: WebSocket | null): WebSocket | null {
    previous?.close();
    return next;
  }

  private connect(start: WebSocket | null): ReplaySubject<ILogEvent | ISystemEvent> {
    const s$ = new ReplaySubject<ILogEvent | ISystemEvent>();

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
          const event = this._parser(e);
          if (event != null) {
            s$.next(event);
          }
        }
      });
    } else {
      s$.next({ type: "system", content: "Not connected" });
    }

    return s$;
  }

  private getParser(settings: LogSettings): (e: MessageEvent<string>) => ILogEvent | ISystemEvent | null {
    const source = settings.sources[0]; // Currently only 1 source can be set
    switch (source.inputFormat) {
      case "csv": {
        const delimiter = source.inputFormat[source.inputFormat.search(/\W/)]; // first non-alphanumeric
        const headers = Papa.parse<string[]>(source.csvFormat, { delimiter: delimiter }).data.find(a => !!a)
        if (!headers) {
          return () => ({ type: "system", content: "No csv headers defined" });
        }

        return (e) => {
          const entry = JSON.parse(e.data) as ILogEntry;
          const splits = Papa.parse<string[]>(entry.entry, { delimiter: delimiter }).data.find(a => !!a);
          if (!splits) {
            return null;
          }

          const result: Record<string, string> = {};

          for (let i = splits.length; i < headers.length; i++) {
            splits.push("[n/a]");
          }

          for (let i = 0; i < headers.length; i++) {
            result[headers[i]] = splits[i];
          }

          return { type: "log", content: result as unknown as IMessage };
        }
      }
      case "json":
        return (e) => {
          const entry = JSON.parse(e.data) as ILogEntry;
          const message = JSON.parse(entry.entry) as IMessage;
          return { type: "log", content: message }
        };
      default:
        throw new Error(`invalid input format ${source.inputFormat}`)
    }
  }
}
