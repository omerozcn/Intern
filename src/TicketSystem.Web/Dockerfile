# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies first so this layer caches independently of source edits.
COPY package.json package-lock.json ./
RUN npm ci

COPY . ./
RUN npm run build

# Runtime stage: nginx serves the built assets and proxies /api to the API container,
# which keeps the SPA and the API on one origin so CORS never enters the picture.
FROM nginx:1.27-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
