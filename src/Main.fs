﻿module Main

open Feliz
open Browser.Dom

let root = ReactDOM.createRoot (document.getElementById "root")
root.render (React.strictMode [ App.App () ])
