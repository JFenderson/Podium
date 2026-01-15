# ============================================
# Stage 1: Build Stage
# ============================================
# Use Node.js 20 Alpine for a smaller image size
FROM node:20-alpine AS build

# Set working directory
WORKDIR /app

# Copy package files first to leverage Docker layer caching
# If package.json/package-lock.json don't change, npm install layer can be reused
COPY package*.json ./

# Install dependencies
# Using ci for faster, reproducible builds in CI/CD environments
RUN npm ci

# Copy the rest of the application source code
COPY . .

# Build the Angular application for production
# This creates optimized, minified bundles in the dist directory
RUN npm run build -- --configuration production

# ============================================
# Stage 2: Runtime Stage with Nginx
# ============================================
# Use nginx alpine for a lightweight production server
FROM nginx:alpine AS runtime

# Copy custom nginx configuration
# This configuration handles Angular routing properly
COPY nginx.conf /etc/nginx/nginx.conf

# Copy the built Angular app from the build stage to nginx html directory
# The output path might be dist/podium-frontend/browser based on Angular 21
COPY --from=build /app/dist/podium-frontend/browser /usr/share/nginx/html

# Expose port 80 for HTTP traffic
EXPOSE 80

# nginx will start automatically as the default CMD
# The container will serve the Angular application
