import React, { useEffect, useState } from 'react'
import * as signalR from "@microsoft/signalr";

export default function Notifications() {
  const [messages, setMessages] = useState([])
  const [userId, setUserId] = useState('')

  useEffect(() => {
    if (!userId) return
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/notificationHub')
      .withAutomaticReconnect()
      .build()
    connection.start().then(() => {
      connection.invoke('Subscribe', userId)
    })
    connection.on('SendNotification', msg => {
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
