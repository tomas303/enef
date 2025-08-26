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
            Html.xdate [
                prop.value "2024-06-15"
                prop.format "dd.mm.yyyy"
                prop.placeholder "Select a date"
                prop.onXChange ( fun (v: string) -> Browser.Dom.console.log $"xdate onDateChange: {v}")
            ]
            Html.xboolean [
                prop.value true
                prop.onXChange ( fun (v: bool) -> Browser.Dom.console.log $"xboolean onBooleanChange: {v}")
            ]
        ]
    ]