module WgEdit

open System
open Feliz
open Elmish
open CustomElements


type FieldType =
    | Str
    | Int
    | Bool
    | Select

type StrField = { 
    Name : string
    Value : string
    HandleChange : string -> unit }

type IntField = {
    Name : string
    Value : int
    HandleChange : int -> unit }

type BoolField = {
    Name : string
    Value : bool
    HandleChange : bool -> unit }

type DateTimeField = {
    Name : string
    Value : DateTime
    HandleChange : DateTime -> unit }

type SelectField = {
    Name : string
    Value : string
    Offer : list<string * string>
    HandleChange : string -> unit }

type Field =
| StrField of StrField
| IntField of IntField
| BoolField of BoolField
| DateTimeField of DateTimeField
| SelectField of SelectField


[<ReactComponent>]
let WgStr (fieldName: string) (value: string) (onChange: string -> unit) =
    Html.xtext [
        prop.key $"{fieldName}"
        prop.classes [ "edit-item" ]
        prop.value value
        prop.onXChange onChange
    ]

[<ReactComponent>]
let WgInt (fieldName: string) (value: int) (onChange: int -> unit) =
    Html.xnumber [
        prop.key $"{fieldName}"
        prop.classes [ "edit-item" ]
        prop.value value
        prop.onXChange onChange
    ]

[<ReactComponent>]
let WgBool (fieldName: string) (value: bool) (onChange: bool -> unit) =
    Html.xboolean [
        prop.key $"{fieldName}"
        prop.classes [ "edit-item" ]
        prop.value value
        prop.onXChange onChange
    ]

[<ReactComponent>]
let WgDateTime (fieldName: string) (value: DateTime) (onChange: DateTime -> unit) =
    let dateString = value.ToString("yyyy-MM-dd")
    Html.xdate [
        prop.key $"{fieldName}"
        prop.classes [ "edit-item" ]
        prop.format "dd.mm.yyyy"
        prop.value dateString
        prop.onXChange (fun (isoString: string) -> 
            match DateTime.TryParse(isoString) with
            | true, dt -> onChange dt
            | false, _ -> ())
    ]

[<ReactComponent>]
let WgSelect (fieldName: string) (selectedId: string) (onSelect: string -> unit) (items: (string * string) list) =
    Html.xselect [
        prop.key $"{fieldName}"  // Unique key combining field name and value
        prop.classes [ "edit-item" ]
        prop.value selectedId
        prop.options items
        prop.onXChange onSelect
    ]

[<ReactComponent>]
let WgEditFields fields =
    let edits =
        fields
        |> List.map (fun field ->
            match field with
            | StrField fld -> WgStr fld.Name fld.Value fld.HandleChange
            | IntField fld -> WgInt fld.Name fld.Value fld.HandleChange
            | BoolField fld -> WgBool fld.Name fld.Value fld.HandleChange
            | DateTimeField fld -> WgDateTime fld.Name fld.Value fld.HandleChange
            | SelectField fld -> WgSelect fld.Name fld.Value fld.HandleChange fld.Offer
        )

    Html.div [
        prop.classes [ "edit-box" ]
        prop.children edits
    ]

[<ReactComponent>]
let WgEdit fields onSave onCancel =
    Html.div [
        WgEditFields fields
        Html.div [
            Html.button [
                prop.text "Cancel"
                prop.onClick (fun _ -> onCancel())
            ]
            Html.button [
                prop.text "Save"
                prop.onClick (fun _ -> onSave())
            ]
        ]
    ]
