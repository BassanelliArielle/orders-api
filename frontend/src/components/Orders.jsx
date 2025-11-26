import React, { useEffect, useState } from 'react'
import axios from 'axios'

export default function Orders(){
  const [orders, setOrders] = useState([])
  const [form, setForm] = useState({ cliente: '', produto: '', valor: 0 })

  const fetch = async () => {
    const res = await axios.get('/orders')
    setOrders(res.data)
  }

  useEffect(()=>{ fetch() }, [])

  const submit = async (e) =>{
    e.preventDefault()
    await axios.post('/orders', { cliente: form.cliente, produto: form.produto, valor: parseFloat(form.valor) })
    setForm({ cliente: '', produto: '', valor: 0 })
    fetch()
  }

  return (
    <div>
      <form onSubmit={submit} className="mb-6 grid grid-cols-3 gap-2">
        <input required value={form.cliente} onChange={e=>setForm({...form, cliente:e.target.value})} placeholder="Cliente" className="p-2 border rounded" />
        <input required value={form.produto} onChange={e=>setForm({...form, produto:e.target.value})} placeholder="Produto" className="p-2 border rounded" />
        <div className="flex gap-2">
          <input required type="number" step="0.01" value={form.valor} onChange={e=>setForm({...form, valor:e.target.value})} placeholder="Valor" className="p-2 border rounded flex-1" />
          <button className="px-4 py-2 bg-blue-600 text-white rounded">Criar</button>
        </div>
      </form>

      <table className="w-full bg-white rounded shadow">
        <thead>
          <tr className="text-left">
            <th className="p-2">Cliente</th>
            <th className="p-2">Produto</th>
            <th className="p-2">Valor</th>
            <th className="p-2">Status</th>
            <th className="p-2">Criado</th>
          </tr>
        </thead>
        <tbody>
          {orders.map(o => (
            <tr key={o.id} className="border-t">
              <td className="p-2">{o.cliente}</td>
              <td className="p-2">{o.produto}</td>
              <td className="p-2">{o.valor}</td>
              <td className="p-2">{o.status}</td>
              <td className="p-2">{new Date(o.dataCriacao).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
