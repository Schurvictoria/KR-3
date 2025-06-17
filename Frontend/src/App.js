import React, { useEffect } from 'react'
import connection from './signalr'
import OrdersList from './components/OrdersList';
import OrderForm from './components/OrderForm';
import Notifications from './components/Notifications';

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
      <h1>Интернет-магазин</h1>
      <OrderForm />
      <OrdersList />
      <Notifications />
    </div>
  )
}

export default App
