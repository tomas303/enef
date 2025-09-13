module PgTest

open Feliz
open Fable.Core.JsInterop
open CustomElements
open ExampleUsage

[<ReactComponent>]
let PgTest() =
    let selectedCountry, setSelectedCountry = React.useState("country2")
    
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
            Html.xbutton [
                prop.buttonText "Custom Button"
                prop.onClick (fun _ -> Browser.Dom.console.log "Custom button clicked!" )
            ]
            // Test with direct attribute
            Interop.reactApi.createElement("x-button", {| text = "Direct Button"; onClick = fun _ -> Browser.Dom.console.log "Direct button!" |})
            Html.p $"Current selection: {selectedCountry}"
            Html.xselect [
                prop.value selectedCountry
                prop.options [  
                        ("country1", "United States")
                        ("country2", "Germany") 
                        ("country3", "France") 
                    ]
                prop.onXChange (fun (v: string) -> 
                    Browser.Dom.console.log $"xselect onSelectChange: {v}"
                    setSelectedCountry v)
            ]
            Html.div [
                MyDataManager()
            ]
            Html.div [
                LayoutShowcase()
            ]
           
        ]
    ]