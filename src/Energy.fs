namespace Energies

open System
open Feliz
open Elmish

type EnergyDbType =
  { ID: Guid
    Amount: int64
    Info: string
    Date: DateTime }

type EnergyEditType =
  { ID: Guid
    Amount: EditUtils.Field
    Info: EditUtils.Field
    Date: EditUtils.Field }


module Utils =
  let amountVD = editIsValid "^\\d+$"

  let createEdit amount info date =
    { ID = Guid.NewGuid()
      Amount =
        { Input = amount.ToString()
          Valid = true }
      Info = { Input = info; Valid = true }
      Date =
        { Input = date.ToString()
          Valid = true } }

  let createEditFromDB energyDB =
    { ID = energyDB.ID
      Amount =
        { Input = energyDB.Amount.ToString()
          Valid = true }
      Info = energyDB.Info
      Date =
        { Input = energyDB.Date.ToString()
          Valid = true } }


module EditEnergy =

  type State = { Energy: EnergyEditType }

  type Msg =
    | SetAmount of string
    | SetInfo of string
    | SetDate of string

  let empty () =
    { Energy = Utils.createEdit 0 "" DateTime.Now }

  let amountVD = editIsValid "^\\d+$"

  let update msg state =
    match msg with
    | SetAmount x ->
      let state = { state with Energy.Amount = { Input = x; Valid = amountVD (x) } }
      state, Cmd.none
    | SetInfo x ->
      let state = { state with Energy.Info = { Input = x; Valid = true } }
      state, Cmd.none
    | SetDate x ->
      let state = { state with Energy.Date = { Input = x; Valid = true } }
      state, Cmd.none

  let renderField label (field: EditUtils.Field) (onchange: string -> unit) =
    Html.div [
      prop.classes [ "field" ]
      prop.children [
        Html.label [ prop.text (label: string) ]
        Html.div [
          prop.classes [ "control" ]
          prop.children [
            Html.input [
              prop.classes [
                if field.Valid then
                  "input"
                else
                  "input is-danger"
              ]
              prop.type' "text"
              prop.placeholder "Amount"
              prop.valueOrDefault (field.Input)
              prop.onChange (onchange)
            ]
          ]
        ]
      ]
    ]

  let render (state: State) (dispatch: Msg -> unit) =
    Html.div [
      renderField "Date" state.Energy.Date (SetDate >> dispatch)
      renderField "Amount" state.Energy.Amount (SetAmount >> dispatch)
      renderField "Info" state.Energy.Info (SetInfo >> dispatch)
    ]

module Energy =

  type State =
    { Edit: EditEnergy.State
      InEdit: bool }

  type Msg =
    | AddNew
    | EditMsg of EditEnergy.Msg

  let init () : State =
    { Edit = EditEnergy.empty ()
      InEdit = false }

  let update msg state =
    match msg with
    | AddNew ->
      let state =
        { state with
            Edit = EditEnergy.empty ()
            InEdit = true }
      state, Cmd.none
    | EditMsg msg ->
      let editState, editCmd = EditEnergy.update msg state.Edit
      { state with Edit = editState }, Cmd.map EditMsg editCmd

  let render (state: State) (dispatch: Msg -> unit) =
    match state.InEdit with
    | true -> EditEnergy.render state.Edit (fun msg -> dispatch(EditMsg msg))
    | _ ->
      Html.div [
        Html.div [
          Html.button [
            prop.text "Add new energy"
            prop.onClick (fun _ -> dispatch AddNew)
            ]
          ]
        ]
