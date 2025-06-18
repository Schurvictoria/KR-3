# Сборка приложения
FROM node:18-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install
COPY . .
RUN npm run build

# Сервер для статики
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
