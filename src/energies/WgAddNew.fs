namespace Energies

open Feliz
open Elmish

[<RequireQualifiedAccess>]
module WgAddNew =

  type State =
    { 
      WgEditSt: WgEdit.State
      LastRows : Deferred<List<Energy>>
      LastEdits: List<Energy>
    }

  type Msg =
  | WgEditMsg of WgEdit.Msg
  | InitPage
  | LoadLastRows of AsyncOperationEvent<Result<List<Energy>, string>>
  | SaveItem of AsyncOperationEvent<Result<Energy, string>>

  let init () : State =
    { 
      LastRows = HasNotStartedYet
      WgEditSt = WgEdit.empty()
      LastEdits = []
    }

  let update msg state =
    match msg with
    | WgEditMsg x ->
      let newstate, newcmd = WgEdit.update x state.WgEditSt
      { state with WgEditSt = newstate}, newcmd
    | Msg.InitPage ->
      state, Cmd.ofMsg (Msg.LoadLastRows StartIt)
    | Msg.LoadLastRows x ->
      match x with
      | StartIt ->
        let state = { state with LastRows = InProgress }
        state, Cmd.fromAsync (Async.map (fun x -> Msg.LoadLastRows(FinishIt(x))) Api.loadLastRows)
      | FinishIt (Ok items) ->
        let state = { state with LastRows = Resolved (items); LastEdits=items }
        state, Cmd.none
      | FinishIt (Error text) ->
        let state = { state with LastRows = Resolved ([]) }
        state, Cmd.none
    | Msg.SaveItem x ->
      match x with
      | StartIt ->
        let asyncSave = (Api.saveItem(WgEdit.stateToEnergy state.WgEditSt))
        state, Cmd.fromAsync (Async.map (fun x -> Msg.SaveItem(FinishIt(x))) asyncSave )
      | FinishIt (Ok item) ->
        let lastedits = state.LastEdits @ [item]
        let lastedits = 
          match List.length(lastedits) with
          | x when x > 10 -> List.skip (x - 10) lastedits
          | _ -> lastedits
        {state with LastEdits = lastedits}, Cmd.none
      | FinishIt (Error text) ->
        state, Cmd.none

  let render (state : State) (dispatch : Msg -> unit) =
    let grid = Render.grid (fun () -> List.map Render.gridRow state.LastEdits)
    let edit = WgEdit.render state.WgEditSt ( fun x -> dispatch (WgEditMsg x) )

    let addButton =
      Html.div [
        prop.classes [ "edit-buttons" ]
        prop.children [
          Html.button [
            prop.classes [ "edit-item" ]
            prop.style [ style.width 50 ]
            prop.text "Add"
            prop.onClick ( fun _ -> dispatch (SaveItem StartIt) )
          ]
        ]
      ]

    [ Html.p(grid); Html.p([edit]); Html.p([addButton])]
