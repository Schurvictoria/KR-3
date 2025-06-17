import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/notificationHub", {
    withCredentials: true,
    skipNegotiation: false,
    transport: signalR.HttpTransportType.WebSockets
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 20000]) // Retry intervals
  .configureLogging(signalR.LogLevel.Debug) // Enable detailed logging
  .build();

// Add connection state logging
connection.onclose(error => {
  console.error("SignalR Connection Error:", error);
});

connection.onreconnecting(error => {
  console.log("SignalR Reconnecting:", error);
});

connection.onreconnected(connectionId => {
  console.log("SignalR Reconnected. ConnectionId:", connectionId);
});

export default connection;
