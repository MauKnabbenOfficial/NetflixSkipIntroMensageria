import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())
  const apiPort = env.VITE_API_PORT ?? '58603'
  const apiBase = `http://localhost:${apiPort}`

  return {
    plugins: [vue()],
    server: {
      port: 3000,
      proxy: {
        // development → IIS Express (58603)  |  production → dotnet run (5091)
        '/api': {
          target: apiBase,
          rewrite: (path) => path.replace(/^\/api/, ''),
          changeOrigin: true
        },
        '/videos': {
          target: apiBase,
          changeOrigin: true
        }
      }
    }
  }
})
