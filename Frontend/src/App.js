import React from 'react';
import OrdersList from './components/OrdersList';
import OrderForm from './components/OrderForm';
import Notifications from './components/Notifications';
import './App.css';

const App = () => {
  return (
    <div className="container">
      <header>
        <h1>🛒 Интернет-магазин</h1>
      </header>
      <main>
        <section className="order-section">
          <OrderForm />
        </section>
        <section className="orders-list-section">
          <OrdersList />
        </section>
        <section className="notifications-section">
          <Notifications />
        </section>
      </main>
      <footer>
        <p>© 2024 Магазин мечты</p>
      </footer>
    </div>
  );
};

export default App;
