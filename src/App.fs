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
  | Overview
  | Pricelists
  | AddNewRecords


type State =
  { WgAddNewSt : Energies.WgAddNew.State 
    WgOverviewSt: Energies.WgList.State
    CurrentPage : Page }

type Msg =
  | WgAddNewMsg of Energies.WgAddNew.Msg
  | WgOverviewMsg of Energies.WgList.Msg
  | SwitchPage of Page
  | Tick of DateTime
  | WindowResized of int * int
  | KeyDown of Browser.Types.KeyboardEvent

let init () =
  let state =
    { WgAddNewSt = Energies.WgAddNew.init()
      WgOverviewSt = Energies.WgList.init()
      CurrentPage = Page.Home }
  let cmd = Cmd.ofMsg (Msg.SwitchPage state.CurrentPage)
  state, cmd

let update msg state =
  match msg with
  | WgAddNewMsg msg ->
    let st, cmd = Energies.WgAddNew.update msg state.WgAddNewSt
    { state with WgAddNewSt = st }, Cmd.map Msg.WgAddNewMsg cmd
  | WgOverviewMsg msg ->
    let st, cmd = Energies.WgList.update msg state.WgOverviewSt
    { state with WgOverviewSt = st }, Cmd.map Msg.WgOverviewMsg cmd
  | SwitchPage page ->
    let st = { state with CurrentPage = page }
    match st.CurrentPage with
    | Page.Home -> st, Page.AddNewRecords |> Msg.SwitchPage |> Cmd.ofMsg
    | Page.AddNewRecords -> st, Energies.WgAddNew.Msg.InitPage |> Msg.WgAddNewMsg |> Cmd.ofMsg
    | Page.Overview -> st, Energies.WgList.Msg.LoadFirstRows StartIt |> Msg.WgOverviewMsg |> Cmd.ofMsg
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
      renderMenuNavItem "Overview" (fun _ -> dispatch (Msg.SwitchPage Page.Overview))
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

let renderOverview (state : State) (dispatch : Msg -> unit) =
  Energies.WgList.render state.WgOverviewSt (Msg.WgOverviewMsg >> dispatch)

let renderHomePage (state : State) (dispatch : Msg -> unit) =
  renderAddNewRecordsPage state dispatch

let render (state : State) (dispatch : Msg -> unit) =
  match state.CurrentPage with
  | Page.Home -> renderApp state dispatch renderHomePage
  | Page.Overview -> renderApp state dispatch renderOverview
  | Page.Pricelists -> Html.div "Pricelists - to be done"
  | Page.AddNewRecords -> renderApp state dispatch renderAddNewRecordsPage

let timer onTick =
  let start dispatch =
      let intervalId = 
          JS.setInterval
              (fun _ -> dispatch (onTick DateTime.Now))
              1000
      { new IDisposable with
          member _.Dispose() = JS.clearInterval intervalId }
  start

// let keydown onKeydown =
//   let start dispatch =
//     // Browser.Document.onKeyDown
//     // Browser.doc  document.addEventListener("keydown", fun e ->
//     //   dispatch (onKeydown e?keyCode))   // use ? operator
//     Document.
//   start

// let subscribe model =
//     [
//         // Listen to window resize events
//         Sub.batch [
//             // window.onresize (fun width height -> WindowResized(width, height))
//             document.onkeydown (fun ev -> KeyPressed ev)
//         ]
//     ]


let keydown onKeydown =
  let start dispatch =

    let handler (event: Event) =
      dispatch (onKeydown (event :?> KeyboardEvent))

    document.addEventListener ("keydown", handler)

    { new IDisposable with
        member _.Dispose() = document.removeEventListener ("keydown", handler) }
  
  // document.onkeydown <- handler
  // document.addEventListener ("keydown", handler)
  start


let subscribe (state : State) =
  Sub.batch [
    Sub.map "WgOverview" WgOverviewMsg (Energies.WgList.subscribe state.WgOverviewSt)
  ]
  // [ ["kvak"], keydown KeyDown ]

// let subscribe (state : State) =
//   [ ["timer"], timer Tick ]
