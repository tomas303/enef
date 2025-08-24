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
let WgStr (value : string) (onChange : string -> unit) =
    // Html.input [
    //     prop.classes [ "edit-item" ]
    //     prop.type' "text"
    //     prop.value value
    //     prop.onChange onChange
    // ]
    Html.xtext [
        prop.classes [ "edit-item" ]
        prop.value value
        prop.onXChange onChange  // Use custom onChange for web components
    ]


[<ReactComponent>]
let WgInt (value : int) (onChange : int -> unit) =
    Html.input [
        prop.classes [ "edit-item" ]
        prop.type' "number"
        prop.value value
        prop.onChange onChange
    ]


[<ReactComponent>]
let WgBool (value : bool) (onChange : bool -> unit) =
    Html.input [
        prop.classes [ "edit-item" ]
        prop.type' "checkbox"
        prop.value value
        prop.onChange onChange
    ]


[<ReactComponent>]
let WgDateTime (value : DateTime) (onChange : DateTime -> unit) =
    Html.input [
        prop.classes [ "edit-item" ]
        prop.type' "date"
        prop.value value
        prop.onChange onChange
    ]


[<ReactComponent>]
let WgSelect (selectedId: string) (onSelect: string -> unit) (items: (string * string) list) =
    Html.select [
        prop.value selectedId
        prop.onChange onSelect
        prop.children (
            items |> List.map (fun (id, name) ->
                Lib.Dbg.wl $"id: {id} name: {name}"
                Html.option [
                    prop.value id
                    prop.text name
                ]
            )
        )
    ]


[<ReactComponent>]
let WgEdit fields onSave onCancel =
    let edits =
        fields
        |> List.map (fun field ->
            match field with
            | StrField fld -> WgStr fld.Value fld.HandleChange
            | IntField fld -> WgInt fld.Value fld.HandleChange
            | BoolField fld -> WgBool fld.Value fld.HandleChange
            | DateTimeField fld -> WgDateTime fld.Value fld.HandleChange
            | SelectField fld -> WgSelect fld.Value fld.HandleChange fld.Offer
        )


    Html.div [
        Html.div [
            prop.classes [ "edit-box" ]
            prop.children edits
        ]

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
