module CustomElements

open Feliz
open Fable.Core.JsInterop

// Extension methods to add custom components directly to Html module
type Html with
    /// Custom text input component without native input
    static member xtext (props: IReactProperty list) =
        Interop.reactApi.createElement("x-text", createObj !!props)
    

// Add custom prop extensions here only when you need properties that don't exist in Feliz
// Custom event handlers for web components
type prop with
    
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
