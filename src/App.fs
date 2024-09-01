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
// | Energy


type State =
  { CurrentPage : Page
    Energy : Energy.State }

type Msg =
  | SwitchPage of Page
  | EnergyMsg of Energy.Msg

let init () =
  let state =
    { CurrentPage = Page.Home
      Energy = Energy.init () }

  let cmd = Cmd.ofMsg (Msg.SwitchPage Page.Home)
  state, cmd

let update msg state =
  match msg with
  | SwitchPage page ->
    let newstate = { state with CurrentPage = page }

    match newstate.CurrentPage with
    // | Page.Energy -> newstate, Cmd.ofMsg (Msg.EnergyMsg (Energy.Msg.InitPage))
    | _ -> newstate, Cmd.none
  | EnergyMsg msg ->
    let energy, cmd = Energy.update msg state.Energy
    { state with Energy = energy }, Cmd.map Msg.EnergyMsg cmd


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

let renderHome (state : State) (dispatch : Msg -> unit) =
  Energy.render state.Energy (Msg.EnergyMsg >> dispatch)

let render (state : State) (dispatch : Msg -> unit) =
  match state.CurrentPage with
  | Page.Home -> renderApp state dispatch renderHome
  | Page.Overview -> Html.div "Overview - to be done"
  | Page.Pricelists -> Html.div "Pricelists - to be done"

