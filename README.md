# Repository
This repository consists of basically 3 parts
1. **NLog.Contrib.Targets.WebSocketServer.Core** - Distributes log events through a websocket
2. **NLog.Contrib.LogListener** - Listens to other log sources
3. **Logair** - A docker container image to use the libraries above right out of the box

## Main intent
The main intention of this solution is a way to **view logs real-time from one or multiple sources**. Regardless if it's a single local application or a bunch of microservices in the cloud.
Collecting log events should ignore what happens with them later on: just show them, write them to a file, pass them to the next collector - this is fully up to your requirements.

Viewing real-time logs should help you to:
* Search errors in your application or multiple services at a single point
* Develop your local or remote application without need to attach a debugger
* Find performance issues in your application or across affected parts


![](https://raw.githubusercontent.com/Reris/NLog.Contrib.Targets.WebSocketServer.Core/master/assets/viewer1.jpg)

The whole solution is not a part or alternative to OpenTelemetry, but can be used as a stepping stone in between.

# NLog.Contrib.Targets.WebSocketServer.Core

Broadcast your servers´ logs to websocket connections in real-time with minimal effort.

Features:

* **Fully integrated with NLog**: it does not require changes in your application code beyond the configuration.
* **Optional integrated log viewer SPA**: To view log events in real-time.
* **Scalable**: NLog and WebSocketServer components are decoupled by a [producer-consumer pattern](http://www.ni.com/white-paper/3023/en/), so NLog will
  append the log entries to `WebSocketTarget` in `O(1)` time always, and another thread/threads are responsible of distributing that log entry to the
  clients. Still, if the component has a big number of connected clients, it may interfere with your application performance. See the `MaxConnectedClients`
  configuration setting. The queue is configured to have a bounding capacity of 1000 items, if the queue gets full, items will start to be discarded.

As said, it includes a SPA-Webclient to view the logs directly, featuring:

* Simplified live log monitoring with a simple web browser app.
* Filter by log levels.
* Deactivate or replace it with your own.
* Not a replacement of a proper monitoring tool, but handy for watching over logs with little logistic effort.

NLog.Contrib.Targets.WebSocketServer.Core is a [NLog target](https://github.com/nlog/nlog/wiki/Targets) that instead of writing on a file, or showing the log on the
console, broadcasts the log events to the connected websocket connections.

## Installation

See _example/NLog.WebSocketServer.Example.Slim_ for the most basic way to use it:
1. To use websockets your applications needs to be a Asp.net Core project
2. Install NLog via NuGet
3. Install NLog.Contrib.Targets.WebSocketServer.Core

### Configuration

To configure NLog the best example is to use the json-messaging protocol in _example/NLog.WebSocketServer.Example.Full/NLog.config_. The compact naming in there is required to use the log viewer SPA.


### NuGet

[NLog.Contrib.Targets.WebSocketServer.Core is available in NuGet](https://www.nuget.org/packages/NLog.Contrib.Targets.WebSocketServer.Core/).

```
PM> Install-Package NLog.Contrib.Targets.WebSocketServer.Core
```

#### Optional configuation parameters:

* `ThrowExceptionIfSetupFails (Boolean)`: By default `NLog.Contrib.Targets.WebSocketServer.Core`-Target will fail silently if does not succeed trying to set up the
  websocket server (e.g.: because the port is already in use), and it will be automatically disabled. In production you may not want the application to crash
  because one of your targets failed, but during development you would like to get an exception indicatig the issue.
* `MaxConnectedClients (Int32)`: The maximum number of allowed connections. By default 100.


## Output

The WebSocket server broadcast JSON objects the way you configured it:

JSON-example (used in Example.Full, recommended way)
```json
{
   "entry":{"@t":"2015-07-16 21:55:38.2576","@l":"INFO","@s":"MyClass","@mn":"MyComputer","@pn":"SampleApplication","@m":"Hello World!"}
}
```
CSV-example (used in Example.Slim)
```json
{
   "entry":"2015-07-16 21:55:38.2576|INFO|MyClass|MyComputer|SampleApplication|Hello World!"
}
```


# NLog.Contrib.LogListener
Listens on a network port to log events from other applications and handles them as local log events.

This allows your application, like the **WebSocketServe.Core** to be a log collector of other, fully independant applications like MicroServices.
Tested log sources are **NLog** and **Serilog**. But due to the slim and open protocol, others can be used as well.

Main requirements of log sources:
1. The need to be able to send formatted logs with a network sink/target
2. The format has to be either compactjson, logstash, log4jxml, log4netxml or a format you would define on your own.

There are serveral examples in **NLog.TcpListener.TestSender**.

## Installation

This is the Listener component. For installation advices of source applications, it's best to take a look on the documentation of your preferred logging library. A connected source application has no code dependencies onto the listener.

To create/install a log listener:
1. It can be used on any kind of .Net projects: Asp.Net Core, Console, etc
2. Install NLog via NuGet
3. Install NLog.Contrib.Targets.WebSocketServer.Core

## Configuration

See _example/NLog.WebSocketServer.Example.Slim/Program.cs_ to look at the way of configuring it by code.
<br>
See _example/NLog.WebSocketServer.Example.Full/Program.cs_ to look at the way of configuring with appsettings.json.

With **appsettings.json**:
```json
{
  [...]
  "LogListener": {
    // "LogInternals": false /* Default: false */,
    "Listeners": [
      {
        // "Ip": "v4" /* Specified IP to clearly adress a network card, or simply "v4" or "v6" to use localhost. Default: v4 */,
        // "Port": "4505" /* Port number to listen to, Default: 4505 */,
        "Formats": [ /* Required: accepted log message formats. Choose whatever you need */
          { "Type": "json", "Schemes": ["compact", "logstash"] },
          { "Type": "log4jxml" },
          { "Type": "log4netxml" }
        ]
      },
      {
        "Ip": "v6",
        "Formats": [
          { "Type": "json" /* Default Scheme: "logstash" */ }
        ]
      }
    ]
  }
}
```

# Logair

Logair is a simple container image of this solution, containing the WebSocketServer.Core implementation and the LogListener.

By default it's configured to listen on port **TCP 4505** and redirect the log messages to the websocket `ws://localhost/wslogger/listen` including the viewer SPA on `http://localhost`

You can install and try it via docker:
```
docker create --name logair -p 4500:80 -p 4505:4505 reris/logair
docker start logair
```
where
* **4500** is your http-port, thus navigating to `http://localhost:4500` should open the viewer
* **4505** is the listener port, here solely to test it via the TestSender tool. Not needed nor recommended to expose it when you just want it for containerized application logs.

Configuration can be done in **NLog.config** and **appsettings-listeners.json** which is by default configured only for logstash formatted log events.

## Helm
The appropriate Helm chart can be found in **Logair/_HelmChart**

### Special thanks to:
* [vtortola WebSocketServer](https://github.com/vtortola/NLog.Contrib.Targets.WebSocketServer/) for his previous solution, which is a foundation stone of this project.
