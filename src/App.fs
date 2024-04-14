[<RequireQualifiedAccess>]
module App

open Elmish
open Feliz

[<RequireQualifiedAccess>]
type Page =
    | Start 


type State = { 
        CurrentPage : Page 
    }

type Msg =
    | SwitchPage of Page

let init() = 
    let state = { CurrentPage = Page.Start }
    let cmd = Cmd.ofMsg (Msg.SwitchPage Page.Start)
    state, cmd

let update msg state =
    match msg with
    | SwitchPage page ->
        let state = { state with CurrentPage = page }
        state, Cmd.none

let render (state: State) (dispatch: Msg -> unit) =
    match state.CurrentPage with
    | Page.Start ->
        Html.div [
            Html.div "STARTING PAGE"
        ]
