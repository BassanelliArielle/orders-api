import React from 'react'
import Orders from './components/Orders'

export default function App(){
  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-4xl mx-auto">
        <h1 className="text-2xl font-bold mb-4">Gestão de Pedidos</h1>
        <Orders />
      </div>
    </div>
  )
}
