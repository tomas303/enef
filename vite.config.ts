import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
    clearScreen: false,
    plugins: [react()],
    server: {
        watch: {
            ignored: [
                "**/*.fs" // Don't watch F# files
            ]
        }
    },
    build: {
        outDir: '../eneb/dist',
        rollupOptions: {
            output: {
                format: 'es', // ESM format
                entryFileNames: 'assets/enef.js',
                chunkFileNames: 'assets/[name].js',
                assetFileNames: '[name][extname]',
                manualChunks: {
                    react: ['react', 'react-dom']
                }
            },
        },        
    }
})

