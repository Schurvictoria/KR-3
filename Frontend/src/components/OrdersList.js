import React, { useState } from 'react'

export default function OrdersList() {
  const [orders, setOrders] = useState([])
  const [userId, setUserId] = useState('')

  const fetchOrders = async () => {
    const res = await fetch(`/orders?userId=${userId}`)
    const data = await res.json()
    setOrders(data)
  }

  return (
    <div>
      <input placeholder='User ID' value={userId} onChange={e => setUserId(e.target.value)} />
      <button onClick={fetchOrders}>Get Orders</button>
      <ul>
        {orders.map(o => (
          <li key={o.id}>{o.description} - {o.amount} - {o.status}</li>
        ))}
      </ul>
    </div>
  )
}
