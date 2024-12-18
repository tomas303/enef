module PgEnergies

open Elmish
open Feliz
open Energies

type State =
  { Activated : bool
    ListSt : Energies.WgList.State
    AddNewSt : Energies.WgAddNew.State }

type Msg =
  | Activate
  | ListMsg of Energies.WgList.Msg
  | AddNewMsg of Energies.WgAddNew.Msg

let init () =
  { Activated = false
    ListSt = Energies.WgList.init () 
    AddNewSt = Energies.WgAddNew.init() }


let update msg state =
  match msg with
  | Activate ->
    match state.Activated with
    | false ->
      { state with Activated = true },
      Energies.WgList.Msg.LoadFirstRows
      |> Msg.ListMsg
      |> Cmd.ofMsg
    | _ -> state, Cmd.none
  | ListMsg msg ->
    let st, cmd = Energies.WgList.update msg state.ListSt
    { state with ListSt = st }, Cmd.map ListMsg cmd
  | AddNewMsg msg ->
    let st, cmd, exmsg = Energies.WgAddNew.update msg state.AddNewSt
    match exmsg with
    | WgAddNew.ExternalMsg.Added x ->
      let cmdx = Cmd.ofMsg (ListMsg (WgList.Msg.Refresh x))
      let cmdy = Cmd.map AddNewMsg cmd
      let cmdw = Cmd.batch [ cmdx; cmdy ]
      { state with AddNewSt = st }, cmdw
    | _ -> 
      { state with AddNewSt = st }, Cmd.map AddNewMsg cmd


let subscribe isactive state =
  Sub.batch [
    Sub.map "list" ListMsg (Energies.WgList.subscribe isactive state.ListSt)
    Sub.map "addnew" AddNewMsg (Energies.WgAddNew.subscribe isactive state.AddNewSt)
  ]

let render (state : State) (dispatch : Msg -> unit) =
  [ Html.div [
      prop.children (        
        List.concat [  
        Energies.WgList.render state.ListSt (ListMsg >> dispatch)
        Energies.WgAddNew.render state.AddNewSt (AddNewMsg >> dispatch)
        ]
      )
    ] ]
