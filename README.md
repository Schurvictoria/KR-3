# Интернет-магазин на микросервисной архитектуре

Проект выполнен в рамках домашнего задания курса "Конструирование программного обеспечения" 2 курса ФКН НИУ ВШЭ
## Описание

Данный проект реализует интернет-магазин с микросервисной архитектурой, поддерживающий асинхронное взаимодействие между сервисами, автоматическую оплату заказов, push-уведомления и масштабируемость.

![image](https://github.com/user-attachments/assets/3c4936f8-d08b-4e6a-8cd3-1376ac05573d)

**Ключевые сервисы:**
- **API Gateway** — маршрутизация всех внешних запросов.
- **Orders Service** — создание и просмотр заказов, асинхронная интеграция с оплатой.
- **Payments Service** — управление счетами пользователей, пополнение, списание, transactional inbox/outbox.
- **Notification Service** — push-уведомления через WebSocket (SignalR).
- **Frontend** — современное React-приложение с красивым UI.

## Архитектура

- Взаимодействие между сервисами через RabbitMQ (очереди: `order-payments`, `payment-results`, `notifications`).
- Используются паттерны transactional outbox (Orders, Payments) и transactional inbox (Payments).
- Все сервисы и базы данных запускаются через Docker Compose.
- Push-уведомления реализованы через SignalR и отображаются на фронте.

## Запуск

1. **Склонируйте репозиторий и перейдите в папку backend:**
   ```sh
   git clone ...
   cd backend
   ```
2. **Запустите все сервисы:**
   ```sh
   docker-compose up --build
   ```
3. **Запустите веб-приложение (фронтенд):**
   - http://localhost:3000
![image](https://github.com/user-attachments/assets/c3cbdf3f-ab11-4f61-8e98-92e210c16725)

## API и документация

- **Swagger** доступен для каждого сервиса:
  - OrdersService: http://localhost:8080/swagger
  - PaymentsService: http://localhost:5001/swagger
  - ApiGateway: http://localhost:8080/swagger

- **Postman-коллекция**: экспортируйте из Swagger UI для тестирования всех эндпоинтов.

## Тесты

- Интеграционные тесты для OrdersService и PaymentsService находятся в папках `OrdersService.Tests` и `PaymentsService.Tests`.
- Запуск тестов:
  ```sh
  dotnet test backend/OrdersService.Tests/OrdersService.Tests.csproj
  dotnet test backend/PaymentsService.Tests/PaymentsService.Tests.csproj
  ```

## Ключевые особенности реализации (критерии):

- Асинхронное создание заказа и автосписание оплаты через transactional outbox/inbox.
- Гарантия доставки событий и отсутствие коллизий при параллельных операциях.
- Push-уведомления о статусе заказа через WebSocket.
- Современный UI на React с красивой стилизацией.
- Все сервисы упакованы в Docker-контейнеры и запускаются одной командой.

## Пример пользовательского сценария

1. Пользователь создаёт заказ через фронтенд.
2. Orders Service сохраняет заказ и событие в outbox.
3. Payments Service через transactional inbox обрабатывает оплату, сохраняет результат в outbox.
4. Notification Service отправляет push-уведомление о статусе заказа.
5. Пользователь видит обновление статуса заказа в реальном времени.


- Проект выполнен в рамках учебного задания по курсу "Конструирование программного обеспечения".
