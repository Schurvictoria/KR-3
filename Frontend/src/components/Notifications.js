import React, { useEffect, useState } from 'react'

export default function Notifications() {
  const [messages, setMessages] = useState([])
  const [userId, setUserId] = useState('')

  useEffect(() => {
    if (!userId) return
    const connection = new window.signalR.HubConnectionBuilder()
      .withUrl('/notificationhub')
      .build()
    connection.start().then(() => {
      connection.invoke('Subscribe', userId)
    })
    connection.on('OrderStatusChanged', msg => {
      setMessages(m => [...m, JSON.stringify(msg)])
    })
    return () => { connection.stop() }
  }, [userId])

  return (
    <div>
      <input placeholder='User ID' value={userId} onChange={e => setUserId(e.target.value)} />
      {messages.map((m, i) => <div key={i}>{m}</div>)}
    </div>
  )
}
