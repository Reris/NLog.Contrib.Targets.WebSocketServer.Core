import { EventEmitter, Injectable } from "@angular/core";
import {
  BehaviorSubject, distinct,
  filter,
  interval,
  map,
  merge,
  Observable,
  pairwise,
  ReplaySubject,
  retry,
  Subject,
  switchMap
} from "rxjs";
import { webSocket, WebSocketSubject } from "rxjs/webSocket";
import * as Papa from "papaparse";

import { Message } from "./Message";
import { LogSettings } from "./LogSettings";
import { LogEvent } from "./LogEvent";
import { LogEntry } from "./LogEntry";
import { SystemEvent } from "./SystemEvent";
import { LogLevel } from "./LogLevel";

@Injectable({
  providedIn: "root"
})
export class LoggerService {
  private static readonly logSettingsStorageKey = "LOG_LOGSETTINGS";
  private static readonly retryMs = 5000;
  public onClear = new EventEmitter();

  private _paused$ = new BehaviorSubject<boolean>(false);
  public paused$ = this._paused$.asObservable();

  private _minLevel$ = new BehaviorSubject<LogLevel>(LogLevel.Trace);
  public minLevel$ = this._minLevel$.asObservable();

  private _settings$ = new BehaviorSubject<LogSettings>(this.restoreLogSettings());
  public settings$ = this._settings$.asObservable();

  private _server$ = new ReplaySubject<string | null>(2);
  private _stream$ = new Subject<LogEvent | SystemEvent>();
  private _websocket$: Observable<WebSocketSubject<LogEvent | SystemEvent>> = this._server$
    .pipe(
      distinct(),
      map((a) => a != null ? ({ ws: this.connect(a), url: a }) : null),
      pairwise(),
      map(([a, b]) => {
        this.disconnect(a?.ws ?? null);
        return b
      }),
      filter(a => a?.ws != null),
      map(a => ({ ws: a!.ws!, url: a!.url! })))
    .pipe(
      map(a => <WebSocketSubject<LogEvent | SystemEvent>>a.ws.pipe(
        filter(b => b != null),
        filter(() => !this._paused$.value),
        map(b => b!),
        retry({
          delay: () => {
            this._stream$.next({
              type: "system",
              content: `could not connect to '${a.url}'. Retry in ${LoggerService.retryMs / 1000} seconds…`
            });
            return interval(LoggerService.retryMs);
          }
        }))));
  public stream$: Observable<LogEvent | SystemEvent> = merge(
    this._stream$,
    this._websocket$.pipe(switchMap(a => a))
  );

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
    if (localStorage) {
      localStorage.setItem(LoggerService.logSettingsStorageKey, JSON.stringify(settings));
    }
    
    this._settings$.next(settings);
  }

  public pause() {
    this._paused$.next(!this._paused$.value);
  }

  public setMinLevel(level: LogLevel) {
    this._minLevel$.next(level);
  }

  private restoreLogSettings(): LogSettings {
    let storedSettings: string | null = null;
    if (localStorage && (storedSettings = localStorage.getItem(LoggerService.logSettingsStorageKey))) {
      return JSON.parse(storedSettings) as LogSettings;
    }

    return new LogSettings()
  }

  private _parser: (e: MessageEvent) => (LogEvent | SystemEvent | null) = () => ({
    type: "system",
    content: "No parser defined"
  });

  private disconnect(previous: WebSocketSubject<LogEvent | SystemEvent | null> | null) {
    previous?.complete();
  }

  private connect(url: string): WebSocketSubject<LogEvent | SystemEvent | null> | null {
    if (url == null) {
      return null;
    }

    this._stream$.next({ type: "system", content: `connecting to '${url}'` });
    return webSocket({
      url: url,
      deserializer: this._parser,
      openObserver: {
        next: () => {
          this._stream$.next({ type: "system", content: `connected to '${url}'` })
        }
      },
      closeObserver: {
        next: e => {
          this._stream$.next({ type: "system", content: `closed '${url}', ${e.code}:${e.reason}` });
        }
      },
    });
  }

  private getParser(settings: LogSettings): (e: MessageEvent<string>) => LogEvent | SystemEvent | null {
    const source = settings.sources[0]; // Currently only 1 source can be set
    switch (source.inputFormat) {
      case "csv": {
        const delimiter = source.inputFormat[source.inputFormat.search(/\W/)]; // first non-alphanumeric
        const headers = Papa.parse<string[]>(source.csvFormat, { delimiter: delimiter }).data.find(a => !!a)
        if (!headers) {
          return () => ({ type: "system", content: "No csv headers defined" });
        }

        return (e) => {
          const entry = JSON.parse(e.data) as LogEntry;
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

          return { type: "log", content: result as unknown as Message };
        }
      }
      case "json":
        return (e) => {
          const entry = JSON.parse(e.data) as LogEntry;
          const message = JSON.parse(entry.entry) as Message;
          return { type: "log", content: message }
        };
      default:
        throw new Error(`invalid input format ${source.inputFormat}`)
    }
  }
}
