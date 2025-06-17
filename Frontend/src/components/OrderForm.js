import React, { useState } from 'react'

export default function OrderForm() {
  const [amount, setAmount] = useState('')
  const [description, setDescription] = useState('')
  const [userId, setUserId] = useState('')
  const [status, setStatus] = useState('')
  const [error, setError] = useState('')

  const handleSubmit = async (e) => {
    e.preventDefault()
    setStatus('loading')
    setError('')
    
    try {
      const response = await fetch('http://localhost:8080/api/orders', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId, amount: Number(amount), description })
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      setStatus('success')
      setAmount('')
      setDescription('')
    } catch (err) {
      console.error('Error creating order:', err)
      setError(err.message)
      setStatus('error')
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <input placeholder='User ID' value={userId} onChange={e => setUserId(e.target.value)} />
        <input placeholder='Amount' value={amount} onChange={e => setAmount(e.target.value)} />
        <input placeholder='Description' value={description} onChange={e => setDescription(e.target.value)} />
        <button type='submit' disabled={status === 'loading'}>
          {status === 'loading' ? 'Creating...' : 'Create Order'}
        </button>
      </div>
      {status === 'success' && <div style={{ color: 'green' }}>Order created successfully!</div>}
      {error && <div style={{ color: 'red' }}>Error: {error}</div>}
    </form>
  )
}
