import { environment } from "../environments/environment";

export class LogSettings {
  public maxRows: number = 0;
  public sources: LogSource[] = [new LogSource()]
  public colors = new Colors();
}

export class Colors {
  public system: string = "#447799";
  public trace: string = "#333333";
  public debug: string = "#000000";
  public info: string = "#006600";
  public warn: string = "#996600";
  public error: string = "#990000";
  public fatal: string = "#990099";
}

export class LogSource {
  public source: string = LogSource.determineServer();
  public inputFormat: "json" | "csv" = "json";
  public csvFormat: string = "date|level|machinename|processname|logger|message";

  private static determineServer() {
    if (environment.production) {
      return `ws://${window.location.host}/wslogger/listen`;
    }

    return "ws://localhost:5095/wslogger/listen";
  }
}
