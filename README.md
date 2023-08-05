# NLog.Contrib.Targets.WebSocketServer.Core

Broadcast your servers´ logs to websocket connections in real-time with minimal effort.

Features:

* **Fully integrated with NLog**: it does not require changes in your application code beyond the configuration.
* **Subscribe to Regular Expressions**: Is it possible to send a message throug the WebSocket connection to indicate the expression to which you want to
  subscribe. Only log entries matching that regex will be sent.
* **Scalable**: NLog and WebSocketListener components are decoupled by a [producer-consumer pattern](http://www.ni.com/white-paper/3023/en/), so NLog will
  append the log entries to `WebSocketServerTarget` in `O(1)` time always, and another thread/threads are responsible of distributing that log entry to the
  clients. Still, if the component has a big number of connected clients, it may interfere with your application performance. See the `MaxConnectedClients`
  configuration setting. The queue is configured to have a bounding capacity of 1000 items, if the queue gets full, items will start to be discarded.

Includes a SPA-Webclient to view the logs directly, featuring:

[//]: # (* Subscribe to a regular expressions to remove undesired lines.)

* Simplified live log monitoring with a simple web browser app.
* Deactivate or replace it with your own.
* Not a replacement of a proper monitoring tool, but handy for watching over logs with little logistic effort.

NLog.Contrib.Targets.WebSocketServer.Core is a [NLog target](https://github.com/nlog/nlog/wiki/Targets) that instead of writing on a file, or showing the log on the
console, broadcast the log entries to the connected websocket connections.

[//]: # ([Checkout this example of a log viewer done in Angular]&#40;//github.com/vtortola/NLog.Contrib.Targets.WebSocketServer/wiki/WebSocket-log-viewer-UI-example-with-AngularJS&#41;. )

[//]: # (![AngularJS Log viewer]&#40;http://vtortola.github.io/NLog.Contrib.Targets.WebSocketServer/screenshot.png&#41;)

## Installation

### NuGet

[NLog.Contrib.Targets.WebSocketServer.Core is available in NuGet](https://www.nuget.org/packages/NLog.Contrib.Targets.WebSocketServer.Core/).

```
PM> Install-Package NLog.Contrib.Targets.WebSocketServer.Core
```

## Configuration

Configure `NLog.Contrib.Targets.WebsocketServer.Core` as a new target.

#### Optional configuation parameters:

* `ThrowExceptionIfSetupFails (Boolean)`: By default `NLog.Contrib.Targets.WebSocketServer.Core`-Target will fail silently if does not succeed trying to set up the
  websocket server (e.g.: because the port is already in use), and it will be automatically disabled. In production you may not want the application to crash
  because one of your targets failed, but during development you would like to get an exception indicatig the issue.
* `MaxConnectedClients (Int32)`: The maximum number of allowed connections. By default 100.

```xml
  <nlog>
    <targets>
      <target type="Console" name="consolelog" />
      <target name="logfile" type="File" fileName="file.txt" />
      
      <!-- Configuration for NLog.Targets.WebSocketServer -->
      <target name="websocket" type="NLog.Contrib.Targets.WebsocketServer.Core" />
    </targets>
    <rules>
      <logger name="*" minlevel="Trace" writeTo="logfile, websocket, consolelog" />
    </rules>
  </nlog>
```

## Output

The WebSocket server broadcast JSON objects like this:

```json
{
   "entry":"2015-07-16 21:55:38.2576|INFO||This is information."
}
```

* `entry` : The actual formatted log entry.

## Input

The component accepts JSON commands to request special behaviours.

#### Filter by Regular Expression

Instructs the component to only send the lines that match a give Regular Expression. Send an empty or null expression to reset it.

```json
{
   "command":"filter",
   "filter": <RegEx>
}
```

### Links

* [Extending NLog](//github.com/nlog/nlog/wiki/Extending%20NLog)
* [WebSocketListener](//vtortola.github.io/WebSocketListener/)
