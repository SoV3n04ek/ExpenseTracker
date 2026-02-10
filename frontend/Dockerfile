# Stage 1: Build
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies
COPY package*.json ./
RUN npm install

# Copy source code and build
COPY . .
RUN npm run build -- --configuration production

# Stage 2: Runtime
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Copy Nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy build output from Stage 1
# Note: Adjusting path to match Angular's production build structure
COPY --from=build /app/dist/expense-tracker-ui/browser .

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
