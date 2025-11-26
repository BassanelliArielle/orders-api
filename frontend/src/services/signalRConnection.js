import * as signalR from "@microsoft/signalr";

let connection = null;

export function createSignalRConnection(apiBaseUrl) {
  if (connection) return connection;

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${apiBaseUrl}/ordersHub`)
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: () => 3000,
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return connection;
}

export async function startConnection() {
  if (!connection) {
    console.warn("SignalR connection was not created before startConnection()");
    return;
  }

  try {
    await connection.start();
    console.log("Connected to SignalR OrdersHub");
  } catch (err) {
    console.error("Failed to connect to SignalR:", err);
    setTimeout(startConnection, 3000);
  }
}

export function onOrderStatusUpdated(callback) {
  if (!connection) return;

  connection.on("OrderStatusUpdated", callback);
}
