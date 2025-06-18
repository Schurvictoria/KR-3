import React, { useEffect, useState } from 'react'
import * as signalR from "@microsoft/signalr";

export default function Notifications() {
  const [messages, setMessages] = useState([])
  const [userId, setUserId] = useState('')
  const [connectionStatus, setConnectionStatus] = useState('disconnected')

  useEffect(() => {
    if (!userId) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/notificationHub')
      .withAutomaticReconnect()
      .build()

    const startConnection = async () => {
      try {
        await connection.start()
        setConnectionStatus('connected')
        await connection.invoke('Subscribe', userId)
      } catch (err) {
        console.error('SignalR Connection Error:', err)
        setConnectionStatus('error')
      }
    }

    connection.on('SendNotification', msg => {
      setMessages(m => [...m, typeof msg === 'string' ? msg : JSON.stringify(msg)])
    })

    connection.onclose(() => {
      setConnectionStatus('disconnected')
    })

    startConnection()

    return () => {
      connection.stop()
    }
  }, [userId])

  return (
    <div className="notifications">
      <div style={{ marginBottom: 8 }}>
        <b>Push-уведомления</b>
        <div style={{ fontSize: 13, color: connectionStatus === 'connected' ? 'green' : 'red' }}>
          Состояние подключения: {connectionStatus}
        </div>
        <input placeholder='Идентификатор пользователя' value={userId} onChange={e => setUserId(e.target.value)} />
      </div>
      <ul>
        {messages.map((m, i) => (
          <li key={i} style={{ color: '#2563eb', marginBottom: 4 }}>{m}</li>
        ))}
      </ul>
    </div>
  )
}
