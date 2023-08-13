export class LogSettings {
  public maxRows: number = 0;
  public sources: LogSource[] = [new LogSource()]
  public colors = new Colors();
}

export class Colors {
  public system: string = "#4488bb";
  public trace: string = "#888888";
  public debug: string = "#dddddd";
  public info: string = "#00aa00";
  public warn: string = "#ffaa00";
  public error: string = "#ff0000";
  public fatal: string = "#cc00cc";
}

export class LogSource {
  public source: string = "self";
  public inputFormat: "json" | "csv" = "csv";
  public csvFormat: string = "date|level|machinename|processname|logger|message";
}
