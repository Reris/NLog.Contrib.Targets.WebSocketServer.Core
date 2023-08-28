import { Message } from "./Message";

export interface LogEvent {
  type: "log";
  content: Message;
}

