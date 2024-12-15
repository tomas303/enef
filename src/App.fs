[<RequireQualifiedAccess>]
module App

open Elmish
open Feliz
open Fable.Core
open System

open Fable.Core.JsInterop
open Browser
open Browser.Types
open Browser.Dom

[<RequireQualifiedAccess>]
type Page =
  | Home
  | Pricelists
  | AddNewRecords
  | Energies


type State =
  { WgAddNewSt : Energies.WgAddNew.State 
    PgEnergiesSt: PgEnergies.State
    CurrentPage : Page }

type Msg =
  | WgAddNewMsg of Energies.WgAddNew.Msg
  | PgEnergiesMsg of PgEnergies.Msg
  | SwitchPage of Page

let init () =
  let state =
    { WgAddNewSt = Energies.WgAddNew.init()
      PgEnergiesSt = PgEnergies.init()
      CurrentPage = Page.Home }
  let cmd = Cmd.ofMsg (Msg.SwitchPage state.CurrentPage)
  state, cmd

let update msg state =
  match msg with
  | WgAddNewMsg msg ->
    let st, cmd = Energies.WgAddNew.update msg state.WgAddNewSt
    { state with WgAddNewSt = st }, Cmd.map Msg.WgAddNewMsg cmd
  | PgEnergiesMsg msg ->
    let st, cmd = PgEnergies.update msg state.PgEnergiesSt
    { state with PgEnergiesSt = st }, Cmd.map Msg.PgEnergiesMsg cmd
  | SwitchPage page ->
    let st = { state with CurrentPage = page }
    match st.CurrentPage with
    | Page.Home -> st, Page.AddNewRecords |> Msg.SwitchPage |> Cmd.ofMsg
    | Page.AddNewRecords -> st, Energies.WgAddNew.Msg.InitPage |> Msg.WgAddNewMsg |> Cmd.ofMsg
    | Page.Energies -> st, PgEnergies.Msg.Activate |> Msg.PgEnergiesMsg |> Cmd.ofMsg
    | _ -> st, Cmd.none

let renderMenuNavItem (label : string) (handler : unit -> unit) =
  Html.a [
    prop.onClick (fun _ -> handler ())
    prop.children [ Html.text label ]
  ]

let renderMenuNav (state : State) (dispatch : Msg -> unit) =
  Html.nav [
    prop.classes [ "menu" ]
    prop.children [
      renderMenuNavItem "Home" (fun _ -> dispatch (Msg.SwitchPage Page.Home))
      renderMenuNavItem "Energies" (fun _ -> dispatch (Msg.SwitchPage Page.Energies))
      renderMenuNavItem "Pricelists" (fun _ -> dispatch (Msg.SwitchPage Page.Pricelists))
    ]
  ]

let renderApp (state : State) (dispatch : Msg -> unit) (renderPage : State -> (Msg -> unit) -> ReactElement list) =
  Html.div [
    prop.classes [ "layout-container" ]
    prop.children [
      Html.div [
        prop.classes [ "layout-nav" ]
        prop.children [
          renderMenuNav state dispatch
        ]
      ]
      Html.div [
        prop.classes [ "layout-content" ]
        prop.children (renderPage state dispatch)
      ]
    ]
  ]

let renderAddNewRecordsPage (state : State) (dispatch : Msg -> unit) =
  Energies.WgAddNew.render state.WgAddNewSt (Msg.WgAddNewMsg >> dispatch)

let renderPgEnergies (state : State) (dispatch : Msg -> unit) =
  PgEnergies.render state.PgEnergiesSt (Msg.PgEnergiesMsg >> dispatch)

let renderHomePage (state : State) (dispatch : Msg -> unit) =
  renderAddNewRecordsPage state dispatch

let render (state : State) (dispatch : Msg -> unit) =
  match state.CurrentPage with
  | Page.Home -> renderApp state dispatch renderHomePage
  | Page.Pricelists -> Html.div "Pricelists - to be done"
  | Page.AddNewRecords -> renderApp state dispatch renderAddNewRecordsPage
  | Page.Energies -> renderApp state dispatch renderPgEnergies

let subscribe (state : State) =
  Sub.batch [
    Sub.map "PgEnergies" PgEnergiesMsg (PgEnergies.subscribe (state.CurrentPage = Page.Energies) state.PgEnergiesSt)
  ]
