[<RequireQualifiedAccess>]
module App

open Elmish
open Feliz

[<RequireQualifiedAccess>]
type Page =
  | Dashboard
  | Energy


type State =
  { CurrentPage: Page
    Energy: Energy.State }

type Msg =
  | SwitchPage of Page
  | EnergyMsg of Energy.Msg

let init () =
  let state =
    { CurrentPage = Page.Dashboard
      Energy = Energy.init () }

  let cmd = Cmd.ofMsg (Msg.SwitchPage Page.Dashboard)
  state, cmd

let update msg state =
  match msg with
  | SwitchPage page ->
    let state = { state with CurrentPage = page }
    state, Cmd.none
  | EnergyMsg msg ->
    let energy, cmd = Energy.update msg state.Energy
    { state with Energy = energy }, cmd

let renderSwitch (state: State) (dispatch: Msg -> unit) (page: ReactElement) =
  Html.div [
    prop.style [ style.padding 20 ]
    prop.children [
      Html.div [
        Html.div [
          prop.className [
            "tabs"
            "is-toggle"
            "is-fullwidth"
          ]
          prop.children [
            Html.ul [
              Html.li [
                prop.onClick (fun _ -> dispatch (Msg.SwitchPage Page.Dashboard))
                prop.children [
                  Html.a [ Html.span ("start") ]
                ]
              ]
              Html.li [
                prop.onClick (fun _ -> dispatch (Msg.SwitchPage Page.Energy))
                prop.children [
                  Html.a [ Html.span ("energy") ]
                ]
              ]
            ]
          ]
        ]
      ]
      Html.div [ page ]
    ]
  ]

let render (state: State) (dispatch: Msg -> unit) =
  match state.CurrentPage with
  | Page.Dashboard ->
    renderSwitch state dispatch (Html.text "STARTING PAGE")
  | Page.Energy ->
    let page = Energy.render state.Energy (fun msg -> dispatch (Msg.EnergyMsg msg))
    renderSwitch state dispatch page
