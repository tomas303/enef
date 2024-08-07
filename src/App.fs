[<RequireQualifiedAccess>]
module App

open Elmish
open Feliz
open Energies

[<RequireQualifiedAccess>]
type Page =
  | Dashboard
  | Energy


type State =
  { CurrentPage : Page
    Energy : Energy.State }

type Msg =
  | SwitchPage of Page
  | EnergyMsg of Energy.Msg

let init () =
  let state =
    { CurrentPage = Page.Dashboard
      Energy = Energy.init () }

  let cmd = Cmd.ofMsg (Msg.SwitchPage Page.Energy)
  state, cmd

let update msg state =
  match msg with
  | SwitchPage page ->
    let newstate = { state with CurrentPage = page }

    match newstate.CurrentPage with
    | Page.Energy -> newstate, Cmd.ofMsg (Msg.EnergyMsg (Energy.Msg.InitPage))
    | _ -> newstate, Cmd.none
  | EnergyMsg msg ->
    let energy, cmd = Energy.update msg state.Energy
    { state with Energy = energy }, Cmd.map Msg.EnergyMsg cmd


let renderPageMenuItem label page currentPage dispatch =
  renderMenuItem label (page = currentPage) (fun _ -> dispatch (Msg.SwitchPage page))

let renderMenu (state : State) (dispatch : Msg -> unit) =
  Html.aside [
    prop.className [ "menu" ]
    prop.children [
      Html.ul [
        prop.className [ "menu-list" ]
        prop.children [
          renderPageMenuItem "Dashboard" Page.Dashboard state.CurrentPage dispatch
          renderPageMenuItem "Energies" Page.Energy state.CurrentPage dispatch
        ]
      ]
    ]
  ]

let renderApp (state : State) (dispatch : Msg -> unit) (pageContent : ReactElement) =
  Html.div [
    prop.className [ "container-fluid" ]
    prop.children [
      Html.div [
        prop.className [ "columns" ]
        prop.children [
          Html.div [
            prop.className [ "column is-one-fifth" ]
            prop.children [
              renderMenu state dispatch
            ]
          ]
          Html.div [
            prop.className [ "column" ]
            prop.children [ pageContent ]
          ]
        ]
      ]
    ]
  ]

let render (state : State) (dispatch : Msg -> unit) =
  match state.CurrentPage with
  | Page.Dashboard -> renderApp state dispatch (Html.text "STARTING PAGE")
  | Page.Energy ->
    let pageContent =
      Energy.render state.Energy (fun msg -> dispatch (Msg.EnergyMsg msg))

    renderApp state dispatch pageContent
