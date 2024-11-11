namespace Energies

open System
open Feliz
open Elmish

[<RequireQualifiedAccess>]
module WgEdit =

  type State = { 
    ID : string
    Kind: EnergyKind
    Amount : int
    Info : string
    Created : DateTime 
  }

  type Msg =
  | Date of float
  | Time of float
  | Kind of string
  | Amount of string
  | Info of string

  let stateToEnergy (state: State): Energy = 
    {
      ID = state.ID
      Kind = state.Kind
      Amount = state.Amount
      Info = state.Info
      Created = (Utils.localDateTimeToUnixTime state.Created)
    }


  let empty () =
    {   ID = Guid.NewGuid().ToString()
        Kind = EnergyKind.Gas
        Amount = 0
        Info = ""
        Created = DateTime.Now
    }

  let update msg state =
    match msg with
    | Msg.Date x ->
      let created = Utils.joinDateAndTime (Utils.jsTimestampToDateTime x) state.Created
      let state = { state with Created = created }
      state, Cmd.none
    | Msg.Time x ->
      let created = Utils.joinDateAndTime state.Created (Utils.jsTimestampToDateTime x)
      let state = { state with Created = created }
      state, Cmd.none
    | Msg.Kind x ->
      match System.Int32.TryParse(x) with
      | (true, intValue) ->
        let state = 
          match Utils.intToEnergyKind(intValue) with
          | Some x -> { state with Kind = x }
          | None -> state
        state, Cmd.none
      | (false, _) -> state, Cmd.none
    | Msg.Amount x ->
      let state = 
        match System.Int32.TryParse x with
        | (true, amount) -> { state with Amount = amount }
        | (false, _) -> state
      state, Cmd.none
    | Msg.Info x ->
      let state = { state with Info = x }
      state, Cmd.none

  let private renderInputs (state : State) (dispatch : Msg -> unit) : Render.Inputs =

    let kindSelectOptions = 
      Constants.EnergyKindToText
      |> Map.toList
      |> List.map ( fun (energyKind, text) -> 
          Html.option [ prop.text text; prop.value Constants.EnergyKindToInt.[energyKind]; prop.selected (state.Kind = energyKind) ] )

    { 
      date = Html.input [
        prop.classes [ "edit-item" ]
        prop.type' "date"
        prop.value (state.Created.ToString("yyyy-MM-dd"))
        prop.onChange (Msg.Date >> dispatch)
      ]
      time = Html.input [
        prop.classes [ "edit-item" ]
        prop.type' "time"
        prop.value (state.Created.ToString("HH:mm"))
        prop.onChange (Msg.Time >> dispatch)
      ]
      kind = Html.select [
        prop.classes [ "edit-item" ]
        prop.children kindSelectOptions
        prop.onChange (Msg.Kind >> dispatch)
      ]
      amount = Html.input [
        prop.classes [ "edit-item" ]
        prop.style [ style.width 50; style.textAlign.right ]
        prop.type' "text" 
        prop.placeholder "amount"
        prop.value (state.Amount.ToString())
        prop.onChange (Msg.Amount >> dispatch)
      ]
      unit = Html.span [
        prop.classes [ "edit-item" ]
        prop.children [
          Html.text Constants.EnergyKindToUnit.[state.Kind]
        ]
      ]
      info = Html.input [ 
        prop.classes [ "edit-item" ] 
        prop.style [ style.custom  ("--flex-grow", "1" ) ]
        prop.type' "text"
        prop.placeholder "remark"
        prop.value (state.Info)
        prop.onChange (Msg.Info >> dispatch)
      ]
    }

  let render (state : State) (dispatch : Msg -> unit) =
    let inputs = renderInputs state dispatch
    Render.edit inputs
