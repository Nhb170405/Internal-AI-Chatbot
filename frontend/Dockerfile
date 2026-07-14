FROM node:22-bookworm-slim

WORKDIR /app

COPY package*.json ./
RUN npm install \
    && npm install @rollup/rollup-linux-x64-gnu --no-save

COPY . .

ENV HOST=0.0.0.0
ENV PORT=5173

EXPOSE 5173

CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
