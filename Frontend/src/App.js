import React, { useEffect } from 'react'
import connection from './signalr'

const App = () => {
  useEffect(() => {
    connection.start().then(() => {
      const userId = localStorage.getItem("userId");
      if (userId) connection.invoke("Subscribe", userId);
    });
    connection.on("SendNotification", (message) => {
      alert(message);
    });
  }, [])

  return (
    <div>
      <h1>Hello world</h1>
    </div>
  )
}

export default App
