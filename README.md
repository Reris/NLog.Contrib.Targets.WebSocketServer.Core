# NLog.Targets.WebSocketServer

Watch your servers´ logs in real-time with minimal effort. Subscribe to a regular expression and focus in what matters.

NLog.Targets.WebSocketServer is a [NLog target](https://github.com/nlog/nlog/wiki/Targets) that instead of writing on a file, or showing the log on the console, broadcast the log entries through websockets, so web clients can watch the log in real-time.

NLog target that setups a standalone WebSocket server and broadcasts the log entries.

Features:
 * Fully integrated with NLog: it does not require changes in your application.
 * Self-hosted: it does not need IIS or any hosting framework.
 * Mono compatible: Both NLog and WebSocketListener are compatible with Mono.
 * Subscribe to Regular Expressions: Is it possible to send a message throug the WebSocket connection to indicate the expression to which you want to subscribe. Only log entries matching that regex will be sent.
 * Scalable: It uses an asynchronous WebSocket server 
 

Checkout this example of a log viewer done in AngularJS. 

## Installation

### NuGet
```
comming soon...
```

### Manually
Just drop the following libraries in your bin folder:
 * NLog.Targets.WebsocketServer
 * vtortola.WebSocketListener
 * vtortola.WebSocketListener.Rfc6455
 * Newtonsoft.Json
 * System.Threading.Tasks.Dataflow

## Configuration
Configure `NLog.Targets.WebsocketServer` as a new target.
#### Required configuration parameters:
 * `port´: Indicates in which connection will the WebSocket server listen for new connections.

#### Optional configuation parameters:
 * `IPAddressStartsWith`: Indicates how the client IP address must start to be accepted. Loopback interface is always accepted. A example value would be `192.168.`. An empty string means that all connections are accepted.

```xml
  <nlog>
    <!-- This part is only required for NLog versions < 4.0  -->
    <extensions>
      <add assembly="NLog.Targets.WebsocketServer"/>
    </extensions>
    
    <targets>
      <target type="Console" name="consolelog" />
      <target name="logfile" type="File" fileName="file.txt" />
      
      <!-- Configuration for NLog.Targets.WebSocketServer -->
      <target name="websocket" type="WebSocket" port="9001"/>
    </targets>
    <rules>
      <logger name="*" minlevel="Trace" writeTo="logfile, websocket, consolelog" />
    </rules>
  </nlog>
```

### Links
 * [Extending NLog](//github.com/nlog/nlog/wiki/Extending%20NLog)
 * [WebSocketListener](//vtortola.github.io/WebSocketListener/)
