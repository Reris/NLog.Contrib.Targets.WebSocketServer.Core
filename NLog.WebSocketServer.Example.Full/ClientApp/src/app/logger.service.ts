import { EventEmitter, Injectable } from "@angular/core";
import {
  BehaviorSubject,
  distinctUntilChanged,
  filter,
  interval,
  map,
  merge,
  Observable,
  of,
  pairwise,
  ReplaySubject,
  retry,
  Subject,
  switchMap
} from "rxjs";
import { webSocket, WebSocketSubject } from "rxjs/webSocket";
import * as Papa from "papaparse";

import { Message } from "./Message";
import { LogSettings, LogSource } from "./LogSettings";
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

  private _server$ = new ReplaySubject<LogSource | null>(2);
  private _stream$ = new Subject<LogEvent | SystemEvent>();
  private _websocket$: Observable<WebSocketSubject<LogEvent | SystemEvent>> = this._server$
    .pipe(
      distinctUntilChanged((a, b) => JSON.stringify(a) == JSON.stringify(b)),
      map((a) => a != null ? ({ ws: this.connect(a), source: a }) : null),
      pairwise(),
      map(([a, b]) => {
        this.disconnect(a?.ws ?? null);
        return b
      }),
      filter(a => a?.ws != null),
      map(a => ({ ws: a!.ws!, source: a!.source })))
    .pipe(
      switchMap(a => {
        return <Observable<WebSocketSubject<LogEvent | SystemEvent>>>of(a.ws.pipe(
          filter(b => b != null),
          filter(() => !this._paused$.value),
          map(b => b!),
          retry({
            delay: () => {
              this._stream$.next({
                type: "system",
                content: `could not connect to '${a.source.source}'. Retry in ${LoggerService.retryMs / 1000} seconds…`
              });
              return interval(LoggerService.retryMs);
            }
          })))
      }));
  public stream$: Observable<LogEvent | SystemEvent> = merge(
    this._stream$,
    this._websocket$.pipe(switchMap(a => a))
  );

  constructor() {
    this._server$.next(null);

    this.settings$.subscribe(a => {
      this._server$.next(a.sources[0]);
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

  private disconnect(previous: WebSocketSubject<LogEvent | SystemEvent | null> | null) {
    previous?.complete();
  }

  private connect(source: LogSource): WebSocketSubject<LogEvent | SystemEvent | null> | null {
    if (source?.source == null) {
      return null;
    }

    this._stream$.next({ type: "system", content: `connecting to '${source.source}'` });
    return webSocket({
      url: source.source,
      deserializer: this.getParser(source),
      openObserver: {
        next: () => {
          this._stream$.next({ type: "system", content: `connected to '${source.source}'` })
        }
      },
      closeObserver: {
        next: e => {
          this._stream$.next({ type: "system", content: `closed '${source.source}', ${e.code}:${e.reason}` });
        }
      },
    });
  }

  private getParser(source: LogSource): (e: MessageEvent<string>) => LogEvent | SystemEvent | null {
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
          let entry: LogEntry;
          try {
            entry = JSON.parse(e.data) as LogEntry;
          } catch (err) {
            this._stream$.next({ type: "system", content: `parsing error '${err}' in entry '${e.data}'` });
            return null;
          }

          try {
            const message = JSON.parse(entry.entry) as Message;
            return { type: "log", content: message }
          } catch (err) {
            this._stream$.next({ type: "system", content: `parsing error '${err}' in message '${entry.entry}'` });
            return null;
          }
        };
      default:
        throw new Error(`invalid input format ${source.inputFormat}`)
    }
  }
}
