[<AutoOpen>]
module EditUtils

open System.Text.RegularExpressions
open Feliz


[<RequireQualifiedAccess>]
type Field<'t> = { Input : 't ; Valid : bool }

let editIsValid (regex : string) (input : string) =
  Regex.IsMatch (input, regex, RegexOptions.Singleline)

let map<'t, 'u> (mapping : 't -> 'u) (input : Field<'t>) : Field<'u> =
  {
    Input = mapping input.Input
    Valid = input.Valid
  }

let renderMenuItem label active (handler : unit -> unit) =
  Html.li [
    prop.onClick (fun _ -> handler ())
    prop.children [
      Html.a [
        if active then
          prop.className [ "is-active" ]
        else
          prop.className []
        prop.children [
          Html.span (label : string)
        ]
      ]
    ]
  ]
