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
      setMessages(m => [...m, JSON.stringify(msg)])
    })

    connection.onclose(error => {
      setConnectionStatus('disconnected')
      console.error('Connection closed:', error)
    })

    startConnection()

    return () => {
      connection.stop()
    }
  }, [userId])

  return (
    <div>
      <div>Connection status: {connectionStatus}</div>
      <input placeholder='User ID' value={userId} onChange={e => setUserId(e.target.value)} />
      {messages.map((m, i) => <div key={i}>{m}</div>)}
    </div>
  )
}
