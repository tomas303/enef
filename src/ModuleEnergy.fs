module Energy

open System
open Feliz
open Elmish

type DbRec =
  { ID: int64
    Amount: int64
    Info: string
    Date: DateTime }

type EditRec =
  { ID: int64
    Amount: EditUtils.Field
    Info: string
    Date: EditUtils.Field }

let amountVD = editIsValid "^\\d+$"

type State = { Data: EditRec; InEdit: bool }

type Msg =
  | Add
  | SetAmount of string


let init () : State =
  { Data =
      { ID = 0
        Amount = { Input = ""; Valid = false }
        Info = ""
        Date = { Input = ""; Valid = false } }
    InEdit = false }

let update msg state =
  match state.InEdit with
  | true -> state, Cmd.none
  | false ->
    let state = { state with InEdit = true }
    state, Cmd.none


let render (state: State) (dispatch: Msg -> unit) =
  match state.InEdit with
  | true ->
    Html.div [
      prop.classes [ "field" ]
      prop.children [
        Html.label [ prop.text "Amount" ]
        Html.div [
          prop.classes [ "control" ]
          prop.children [
            Html.input [
              prop.classes [
                if state.Data.Amount.Valid then
                  "input"
                else
                  "input is-danger"
              ]
              prop.type' "text"
              prop.placeholder "Amount"
              prop.valueOrDefault (state.Data.Amount.Input)
              prop.onChange (SetAmount >> dispatch)
            ]
          ]
        ]
      ]
    ]

  | _ -> Html.div [ Html.div "not in edit" ]
