import React, { useState } from 'react'

export default function OrdersList() {
  const [orders, setOrders] = useState([])
  const [userId, setUserId] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const fetchOrders = async () => {
    if (!userId) {
      setError('Please enter a User ID')
      return
    }

    setLoading(true)
    setError('')
    
    try {
      const res = await fetch(`http://localhost:8080/api/orders/user/${userId}`)
      if (!res.ok) {
        throw new Error(`HTTP error! status: ${res.status}`)
      }
      const data = await res.json()
      setOrders(data)
    } catch (err) {
      console.error('Error fetching orders:', err)
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <div>
        <input 
          placeholder='User ID' 
          value={userId} 
          onChange={e => setUserId(e.target.value)} 
        />
        <button 
          onClick={fetchOrders} 
          disabled={loading}
        >
          {loading ? 'Loading...' : 'Get Orders'}
        </button>
      </div>
      
      {error && <div style={{ color: 'red' }}>Error: {error}</div>}
      
      {orders.length === 0 && !loading && !error && (
        <div>No orders found</div>
      )}
      
      <ul>
        {orders.map(o => (
          <li key={o.id}>
            {o.description} - {o.amount} - {o.status}
          </li>
        ))}
      </ul>
    </div>
  )
}
