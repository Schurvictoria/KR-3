# Сборка приложения
FROM node:18-alpine
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install
COPY . .
EXPOSE 3000
CMD ["npm", "start"]

# Сервер для статики
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
