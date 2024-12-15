module PgEnergies

open Elmish
open Feliz

type State =
  { Activated : bool
    ListSt : Energies.WgList.State }

type Msg =
  | Activate
  | ListMsg of Energies.WgList.Msg

let init () =
  { Activated = false
    ListSt = Energies.WgList.init () }


let update msg state =
  match msg with
  | Activate ->
    match state.Activated with
    | false ->
      { state with Activated = true },
      Energies.WgList.Msg.LoadFirstRows StartIt
      |> Msg.ListMsg
      |> Cmd.ofMsg
    | _ -> state, Cmd.none
  | ListMsg msg ->
    let st, cmd = Energies.WgList.update msg state.ListSt
    { state with ListSt = st }, Cmd.map ListMsg cmd


let subscribe isactive state =
  Sub.batch [
    Sub.map "list" ListMsg (Energies.WgList.subscribe isactive state.ListSt)
  ]

let render (state : State) (dispatch : Msg -> unit) =
  [ Html.div [
      prop.children (Energies.WgList.render state.ListSt (ListMsg >> dispatch))
    ] ]
