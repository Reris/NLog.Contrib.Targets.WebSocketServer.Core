import { IMessage } from "./IMessage";

export interface ILogEvent {
  type: "log";
  content: IMessage;
}

