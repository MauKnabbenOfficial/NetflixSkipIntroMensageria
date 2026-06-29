# ── Stage 1: build ────────────────────────────────────────────────────────────
# Usa Node para instalar dependências e gerar os assets estáticos do Vue.
FROM node:20-alpine AS build

WORKDIR /app

# Copia apenas os manifests primeiro (camada de cache)
COPY package*.json ./
RUN npm install

# Copia o resto e faz o build de produção
COPY . .
RUN npm run build
# Resultado: /app/dist/

# ── Stage 2: serve ────────────────────────────────────────────────────────────
# Nginx serve os arquivos estáticos e faz proxy reverso para a API.
FROM nginx:1.25-alpine

# Remove a config padrão do nginx
RUN rm /etc/nginx/conf.d/default.conf

# Copia nossa config (proxy /api/ e /videos/ para netflix-api)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copia os assets gerados pelo Vite
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
