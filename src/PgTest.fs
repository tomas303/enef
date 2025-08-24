module PgTest

open Feliz
open Fable.Core.JsInterop
open CustomElements
// open Fable.React

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
            // Now you can use Html.xtext with clean prop syntax!
            Html.xtext "This is a custom x-text component."
            Html.xtext ("Another custom component", "Enter text here...")
            Html.xtext [
                prop.value "Full control"
                prop.placeholder "Custom placeholder"
                prop.disabled true
            ]
        ]
    ]