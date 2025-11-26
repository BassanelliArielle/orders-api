import { useEffect, useState } from "react";
import {
  createSignalRConnection,
  startConnection,
  onOrderStatusUpdated,
} from "../services/signalRConnection";

export default function Orders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);

  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

  async function fetchOrders() {
    try {
      const res = await fetch(`${apiBaseUrl}/orders`);
      const data = await res.json();
      setOrders(data);
    } catch (err) {
      console.error("Erro ao carregar pedidos:", err);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    fetchOrders(); 

    const connection = createSignalRConnection(apiBaseUrl);

    onOrderStatusUpdated((updatedOrder) => {
      console.log("Atualização do SignalR:", updatedOrder);

      setOrders((prev) =>
        prev.map((o) => (o.id === updatedOrder.id ? updatedOrder : o))
      );
    });

    startConnection();

    return () => {
      if (connection) connection.stop();
    };
  }, []);

  if (loading) return <p>Carregando pedidos...</p>;

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold mb-4">Pedidos</h1>

      <table className="min-w-full bg-white shadow-lg rounded-lg">
        <thead>
          <tr>
            <th className="border px-4 py-2 text-left">Cliente</th>
            <th className="border px-4 py-2 text-left">Produto</th>
            <th className="border px-4 py-2 text-left">Valor</th>
            <th className="border px-4 py-2 text-left">Status</th>
            <th className="border px-4 py-2 text-left">Criado em</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((order) => (
            <tr key={order.id} className="hover:bg-gray-50">
              <td className="border px-4 py-2">{order.client}</td>
              <td className="border px-4 py-2">{order.product}</td>
              <td className="border px-4 py-2">
                R${order.value.toLocaleString()}
              </td>
              <td className="border px-4 py-2">
                <span
                  className={`px-2 py-1 rounded text-white ${
                    order.status === "Pendente"
                      ? "bg-yellow-500"
                      : order.status === "Processando"
                      ? "bg-blue-500"
                      : "bg-green-600"
                  }`}
                >
                  {order.status}
                </span>
              </td>
              <td className="border px-4 py-2">
                {new Date(order.data_criacao).toLocaleString()}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
