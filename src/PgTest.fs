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
            Html.xtext [
                prop.value "Full control"
                prop.placeholder "Custom placeholder"
                // prop.disabled true
                prop.onXChange ( fun (v: string) -> Browser.Dom.console.log $"xtext onTextChange: {v}")
            ]
            Html.xnumber [
                prop.value 42
                prop.placeholder "Enter a number"
                prop.decimalPlaces 2
                prop.onXChange ( fun (v: float) -> Browser.Dom.console.log $"xnumber onNumberChange: {v}")
            ]
        ]
    ]