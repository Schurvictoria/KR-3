import React, { useState } from 'react'

export default function OrderForm() {
  const [amount, setAmount] = useState('')
  const [description, setDescription] = useState('')
  const [userId, setUserId] = useState('')

  const handleSubmit = async (e) => {
    e.preventDefault()
    await fetch('/orders', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userId, amount: Number(amount), description })
    })
    setAmount('')
    setDescription('')
  }

  return (
    <form onSubmit={handleSubmit}>
      <input placeholder='User ID' value={userId} onChange={e => setUserId(e.target.value)} />
      <input placeholder='Amount' value={amount} onChange={e => setAmount(e.target.value)} />
      <input placeholder='Description' value={description} onChange={e => setDescription(e.target.value)} />
      <button type='submit'>Create Order</button>
    </form>
  )
}
