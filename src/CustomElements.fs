module CustomElements

open Feliz
open Fable.Core.JsInterop

// Extension methods to add custom components directly to Html module
type Html with
    /// Custom text input component without native input
    static member xtext (props: IReactProperty list) =
        Interop.reactApi.createElement("x-text", createObj !!props)
    
    /// Custom text input with just a value
    static member xtext (value: string) =
        Html.xtext [ prop.value value ]
    
    /// Custom text input with value and placeholder
    static member xtext (value: string, placeholder: string) =
        Html.xtext [ 
            prop.value value
            prop.placeholder placeholder 
        ]


// Add custom prop extensions here only when you need properties that don't exist in Feliz
// Example for future use:
// type prop with
//     static member customAttribute (value: 'T) = prop.custom("custom-attribute", value)