import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 3000,
    proxy: {
      // Em dev local: redireciona /api/* → http://localhost:5000/*
      '/api': {
        target: 'http://localhost:58603',
        rewrite: (path) => path.replace(/^\/api/, ''),
        changeOrigin: true
      },
      // Vídeos servidos pela API via static files
      '/videos': {
        target: 'http://localhost:58603',
        changeOrigin: true
      }
    }
  }
})
