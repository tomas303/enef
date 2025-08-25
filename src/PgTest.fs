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
                // prop.onChange (fun (val: string) -> Browser.Dom.console.log "Input changed!")
                // prop.onChange ( fun (e: Browser.Types.Event) -> Browser.Dom.console.log "xtext changed!")
                // prop.onInput ( fun (e: Browser.Types.Event) -> Browser.Dom.console.log "xtext input changed!")
                // prop.onTextChange ( fun (text: string) -> Browser.Dom.console.log $"xtext onTextChange: {text}" )
                prop.onXChange ( fun (v: string) -> Browser.Dom.console.log $"xtext onTextChange: {v}")
                // prop.onInput(fun (e: Browser.Types.Event) -> 
                //     let target = e.target :?> Browser.Types.HTMLElement
                //     let value = target?value
                //     Browser.Dom.console.log value)
            ]
        ]
    ]