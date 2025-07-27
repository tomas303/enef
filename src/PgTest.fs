module PgTest

open Feliz
open Fable.Core.JsInterop
open Fable.React

[<ReactComponent>]
let PgTest() =
    Html.div [
        prop.children [
            Html.h1 "Test Page"
            Html.p "This is a test page for demonstration purposes."
            Html.button [
                prop.text "Click Me"
                prop.onClick (fun _ -> Browser.Dom.console.log "Button clicked!")
            ]
            Interop.reactApi.createElement("x-text", createObj [
                "value" ==> "This is a custom x-text component."
            ])
        ]
    ]