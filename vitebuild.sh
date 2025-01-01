#!/bin/bash
if [ "$1" = "fordebug" ]; then     
    export NODE_ENV=development
    dotnet fable src --run npx vite build --mode development --sourcemap --minify false
else
    dotnet fable src --run npx vite build
fi

