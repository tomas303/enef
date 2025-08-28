module CustomElements

open Feliz
open Fable.Core.JsInterop

// Add custom prop extensions first so they can be used in Html extensions
type prop with
    /// Custom property for decimal places in x-number component
    static member decimalPlaces (places: int) = prop.custom("decimal-places", places)
    
    /// Custom property for date format in x-date component (e.g., "dd.mm.yyyy", "mm/dd/yyyy")
    static member format (format: string) = prop.custom("format", format)
    
    /// Custom property for day in x-date component
    static member day (day: int) = prop.custom("day", day)
    
    /// Custom property for month in x-date component  
    static member month (month: int) = prop.custom("month", month)
    
    /// Custom property for year in x-date component
    static member year (year: int) = prop.custom("year", year)
    
    /// Custom property for options in x-select component (list of value-text pairs)
    static member options (options: (string * string) list) = 
        let jsonOptions = 
            options 
            |> List.map (fun (value, text) -> {| value = value; text = text |})
            |> Fable.Core.JS.JSON.stringify
        prop.custom("options", jsonOptions)
    
    /// Custom property for button text in x-button component
    static member buttonText (text: string) = prop.custom("text", text)
    
    /// <summary>
    /// in react onChange is not called for web components, so we create onXChange(some peculiarity of its
    /// syntetic event system - for known text inputs element it does similarly )
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    static member onXChange (handler: string -> unit) = 
        prop.onInput(fun (e: Browser.Types.Event) -> 
            let target = e.target :?> Browser.Types.HTMLElement
            let value = target?value
            handler (string value))
    
    /// Custom onXChange handler for integer values
    static member onXChange (handler: int -> unit) = 
        prop.onInput(fun (e: Browser.Types.Event) -> 
            let target = e.target :?> Browser.Types.HTMLElement
            let value = target?value
            match System.Int32.TryParse(string value) with
            | true, intValue -> handler intValue
            | false, _ -> ()) // Ignore invalid integers
    
    /// Custom onXChange handler for float values
    static member onXChange (handler: float -> unit) = 
        prop.onInput(fun (e: Browser.Types.Event) -> 
            let target = e.target :?> Browser.Types.HTMLElement
            let value = target?value
            match System.Double.TryParse(string value) with
            | true, floatValue -> handler floatValue
            | false, _ -> ()) // Ignore invalid floats
    
    /// Custom onXChange handler for boolean values
    static member onXChange (handler: bool -> unit) = 
        prop.onInput(fun (e: Browser.Types.Event) -> 
            let target = e.target :?> Browser.Types.HTMLElement
            let value = target?value
            let boolValue = string value = "true"
            handler boolValue)

// Extension methods to add custom components directly to Html module
type Html with
    /// Custom text input component without native input
    static member xtext (props: IReactProperty list) =
        Interop.reactApi.createElement("x-text", createObj !!props)
    
    /// Custom number input component with fixed decimal point
    static member xnumber (props: IReactProperty list) =
        Interop.reactApi.createElement("x-number", createObj !!props)
    
    /// Custom date input component with day/month/year parts
    static member xdate (props: IReactProperty list) =
        Interop.reactApi.createElement("x-date", createObj !!props)
    
    /// Custom boolean input component with O/X display
    static member xboolean (props: IReactProperty list) =
        Interop.reactApi.createElement("x-boolean", createObj !!props)
    
    /// Custom select input component with filtering and dropdown
    static member xselect (props: IReactProperty list) =
        Interop.reactApi.createElement("x-select", createObj !!props)
    
    /// Custom button component with consistent styling
    static member xbutton (props: IReactProperty list) =
        Interop.reactApi.createElement("x-button", createObj !!props)
    
    /// Custom number input with just a value (integer only)
    static member xnumber (value: int) =
        Html.xnumber [ prop.value (string value) ]
    
    /// Custom number input with float value and decimal places
    static member xnumber (value: float, decimalPlaces: int) =
        Html.xnumber [ 
            prop.value (string value)
            prop.decimalPlaces decimalPlaces 
        ]
    
    /// Custom date input with ISO date value (YYYY-MM-DD)
    static member xdate (value: string) =
        Html.xdate [ prop.value value ]
    
    /// Custom date input with format and value
    static member xdate (value: string, format: string) =
        Html.xdate [
            prop.value value
            prop.format format
        ]
    
    /// Custom boolean input with boolean value
    static member xboolean (value: bool) =
        Html.xboolean [ prop.value (if value then "true" else "false") ]
    
    /// Custom select input with options and value
    static member xselect (value: string, options: (string * string) list) =
        Html.xselect [
            prop.value value
            prop.options options
        ]
    
    /// Custom button with text and click handler
    static member xbutton (text: string, onClick: unit -> unit) =
        Html.xbutton [
            prop.buttonText text
            prop.onClick (fun _ -> onClick())
        ]
