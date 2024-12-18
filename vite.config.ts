import { defineConfig } from 'vite'

// https://vitejs.dev/config/
export default defineConfig({
    clearScreen: false,
    server: {
        watch: {
            ignored: [
                "**/*.fs" // Don't watch F# files
            ]
        }
    },
    build: {
        outDir: '../eneb/dist', // Replace with your desired directory
        rollupOptions: {
            output: {
              entryFileNames: 'assets/enef.js', // For JavaScript entry files
              chunkFileNames: '[name].js', // For code-split chunks
              assetFileNames: '[name][extname]', // For other assets like CSS or images
            },
        },        
    }
})