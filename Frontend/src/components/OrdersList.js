import React, { useState } from 'react'

export default function OrdersList() {
  const [orders, setOrders] = useState([])
  const [userId, setUserId] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const fetchOrders = async () => {
    if (!userId) {
      setError('Пожалуйста, введите идентификатор пользователя')
      return
    }

    setLoading(true)
    setError('')
    
    try {
      const res = await fetch(`http://localhost:8080/api/orders/user/${userId}`)
      if (!res.ok) {
        throw new Error(`Ошибка HTTP! статус: ${res.status}`)
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
    <div className="orders-list">
      <div style={{ marginBottom: 8 }}>
        <input 
          placeholder='Идентификатор пользователя' 
          value={userId} 
          onChange={e => setUserId(e.target.value)} 
        />
        <button 
          onClick={fetchOrders} 
          disabled={loading}
        >
          {loading ? 'Загрузка...' : 'Получать Заказы'}
        </button>
      </div>
      
      {error && <div style={{ color: 'red', marginBottom: 8 }}>{error}</div>}
      
      {orders.length === 0 && !loading && !error && (
        <div style={{ color: '#a0aec0' }}>Заказы не найдены</div>
      )}
      
      <ul>
        {orders.map(o => (
          <li key={o.id}>
            <b>{o.description}</b> — {o.amount} ₽ — <span style={{ color: o.status === 'Completed' ? 'green' : o.status === 'Cancelled' ? 'red' : '#2d3748' }}>{o.status}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}
