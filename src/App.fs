[<RequireQualifiedAccess>]
module App

open Elmish
open Feliz
open Energies

[<RequireQualifiedAccess>]
type Page =
  | Home
  | Overview
  | Pricelists
  | Editation


type State =
  { WgAddNewSt : WgAddNew.State 
    CurrentPage : Page }

type Msg =
  | WgAddNewMsg of WgAddNew.Msg
  | SwitchPage of Page

let init () =
  let state =
    { WgAddNewSt = WgAddNew.init ()
      CurrentPage = Page.Home }
  let cmd = Cmd.ofMsg (Msg.SwitchPage state.CurrentPage)
  state, cmd

let update msg state =
  match msg with
  | WgAddNewMsg msg ->
    let energy, cmd = WgAddNew.update msg state.WgAddNewSt
    { state with WgAddNewSt = energy }, Cmd.map Msg.WgAddNewMsg cmd
  | SwitchPage page ->
    let newstate = { state with CurrentPage = page }
    match newstate.CurrentPage with
    | Page.Home -> newstate, Page.Editation |> Msg.SwitchPage |> Cmd.ofMsg
    | Page.Editation -> newstate, WgAddNew.Msg.InitPage |> Msg.WgAddNewMsg |> Cmd.ofMsg
    | _ -> newstate, Cmd.none

let renderMenuNavItem (label : string) (handler : unit -> unit) =
  Html.p [
    prop.classes [
      "level-item"
      "has-text-centered"
    ]
    prop.children [
      Html.a [
        prop.classes [ "link is-info" ]
        prop.onClick (fun _ -> handler ())
        prop.children [ Html.text label ]
      ]
    ]
  ]

let renderMenuNav (state : State) (dispatch : Msg -> unit) =
  Html.section [
    prop.classes [ "section" ]
    prop.children [
      Html.nav [
        prop.classes [ "level" ]
        prop.children [
          renderMenuNavItem "Home" (fun _ -> dispatch (Msg.SwitchPage Page.Home))
          renderMenuNavItem "Overview" (fun _ -> dispatch (Msg.SwitchPage Page.Overview))
          renderMenuNavItem "Pricelists" (fun _ -> dispatch (Msg.SwitchPage Page.Pricelists))
        ]
      ]
    ]
  ]

let renderApp (state : State) (dispatch : Msg -> unit) (renderPage : State -> (Msg -> unit) -> ReactElement list) =
  Html.div [
    renderMenuNav state dispatch
    Html.section [
      prop.classes [ "section" ]
      prop.children [
        Html.div [
          prop.classes [ "container" ]
          prop.children (renderPage state dispatch)
        ]
      ]
    ]
  ]

let renderEditation (state : State) (dispatch : Msg -> unit) =
  WgAddNew.render state.WgAddNewSt (Msg.WgAddNewMsg >> dispatch)

let renderHome (state : State) (dispatch : Msg -> unit) =
  renderEditation state dispatch

let render (state : State) (dispatch : Msg -> unit) =
  match state.CurrentPage with
  | Page.Home -> renderApp state dispatch renderHome
  | Page.Overview -> Html.div "Overview - to be done"
  | Page.Pricelists -> Html.div "Pricelists - to be done"
  | Page.Editation -> renderApp state dispatch renderEditation

