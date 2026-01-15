# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY podium-frontend/package*.json ./

# Install dependencies
RUN npm ci --only=production && npm cache clean --force

# Copy application files
COPY podium-frontend/ .

# Build the application
RUN npm run build

# Runtime stage - nginx
FROM nginx:alpine AS final

# Create non-root user
RUN addgroup -g 1001 -S podiumgroup && \
    adduser -u 1001 -S podiumuser -G podiumgroup

# Copy nginx configuration
COPY <<EOF /etc/nginx/nginx.conf
user podiumuser;
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /tmp/nginx.pid;

events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    log_format main '\$remote_addr - \$remote_user [\$time_local] "\$request" '
                    '\$status \$body_bytes_sent "\$http_referer" '
                    '"\$http_user_agent" "\$http_x_forwarded_for"';

    access_log /var/log/nginx/access.log main;

    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;
    client_max_body_size 20M;

    gzip on;
    gzip_disable "msie6";
    gzip_vary on;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml text/javascript application/json application/javascript application/xml+rss application/rss+xml font/truetype font/opentype application/vnd.ms-fontobject image/svg+xml;

    server {
        listen 80;
        server_name _;
        root /usr/share/nginx/html;
        index index.html;

        # Security headers
        add_header X-Frame-Options "DENY" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header X-XSS-Protection "1; mode=block" always;
        add_header Referrer-Policy "strict-origin-when-cross-origin" always;

        location / {
            try_files \$uri \$uri/ /index.html;
        }

        # Cache static assets
        location ~* \.(jpg|jpeg|png|gif|ico|css|js|svg|woff|woff2|ttf|eot)$ {
            expires 1y;
            add_header Cache-Control "public, immutable";
        }

        # Health check endpoint
        location /health {
            access_log off;
            return 200 "healthy\n";
            add_header Content-Type text/plain;
        }
    }
}
EOF

# Copy built application from build stage
COPY --from=build /app/dist/podium-frontend/browser /usr/share/nginx/html

# Set correct permissions
RUN chown -R podiumuser:podiumgroup /usr/share/nginx/html && \
    chown -R podiumuser:podiumgroup /var/cache/nginx && \
    chown -R podiumuser:podiumgroup /var/log/nginx && \
    chown -R podiumuser:podiumgroup /etc/nginx/nginx.conf && \
    chmod -R 755 /usr/share/nginx/html && \
    touch /var/run/nginx.pid && \
    chown -R podiumuser:podiumgroup /var/run/nginx.pid

# Switch to non-root user
USER podiumuser

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
