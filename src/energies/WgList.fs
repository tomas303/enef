namespace Energies

open Feliz
open Elmish
open System
open Fable.Core.JsInterop
open Browser
open Browser.Types
open Browser.Dom

/// <summary>
/// widget for list energy records, edit and delete them
/// </summary>
[<RequireQualifiedAccess>]
module WgList =

    type State = {
        Rows : Deferred<List<Energy>>
        DispRows: List<Energy>
        Limit: int
    }

    type Msg =
    | LoadFirstRows of AsyncOperationEvent<Result<List<Energy>, string>>
    | LoadPrevRows of AsyncOperationEvent<Result<List<Energy>, string>>
    | LoadNextRows of AsyncOperationEvent<Result<List<Energy>, string>>
    | KeyDown of Browser.Types.KeyboardEvent

    let getNextPin state = 
        if state.DispRows.Length > 0 then 
            let energy = state.DispRows.[state.DispRows.Length - 1]
            (energy.Created, energy.ID)
        else (0, "")

    let getPrevPin state = 
        if state.DispRows.Length > 0 then
            let energy = state.DispRows.[0]
            (energy.Created, energy.ID)
        else (0, "")

    let getNextCmd pin limit =
        Cmd.fromAsync (Async.map (fun x -> Msg.LoadNextRows(FinishIt(x))) (Api.loadNextRows pin limit))

    let getPrevCmd pin limit =
        Cmd.fromAsync (Async.map (fun x -> Msg.LoadPrevRows(FinishIt(x))) (Api.loadPrevRows pin limit))

    let init () =
        let limit = 15
        { 
            Rows = HasNotStartedYet
            DispRows = []
            Limit = limit
        }

    let keydown onKeydown =
        let run dispatch =
            let handler (event: Event) = 
                let kev = event :?> KeyboardEvent
                if kev.key = "F2" then
                    kev.preventDefault()
                    dispatch (onKeydown (kev))
            document.addEventListener ("keydown", handler)
            { new IDisposable with
                member _.Dispose() = document.removeEventListener ("keydown", handler) }
        run

    let subscribe state =
        [ ["keydown"], keydown Msg.KeyDown ]

    let update msg state =
        match msg with
        | Msg.LoadFirstRows x ->
            match x with
            | StartIt ->
                let state = { state with Rows = InProgress }
                state, getNextCmd (0,"") state.Limit
            | FinishIt (Ok items) ->
                let state = 
                    if items.Length > 0
                    then { state with Rows = Resolved(items); DispRows=items } 
                    else { state with Rows = Resolved(items) } 
                state, Cmd.none
            | FinishIt (Error text) ->
                let state = { state with Rows = Resolved([]) }
                state, Cmd.none
        | Msg.LoadPrevRows x ->
            match x with
            | StartIt ->
                let newpin = getPrevPin state
                let state = { state with Rows = InProgress }
                state, getPrevCmd newpin state.Limit
            | FinishIt (Ok items) ->
                let state = 
                    if items.Length > 0
                    then { state with Rows = Resolved(items); DispRows= List.truncate state.Limit (List.append items state.DispRows) } 
                    else { state with Rows = Resolved(items) } 
                state, Cmd.none
            | FinishIt (Error text) ->
                let state = { state with Rows = Resolved([]) }
                state, Cmd.none
        | Msg.LoadNextRows x ->
            match x with
            | StartIt ->
                let newpin = getNextPin state
                let state = { state with Rows = InProgress }
                state, getNextCmd newpin state.Limit
            | FinishIt (Ok items) ->
                let state = 
                    if items.Length > 0
                    then { state with Rows = Resolved(items); DispRows=items } 
                    else { state with Rows = Resolved(items) } 
                state, Cmd.none
            | FinishIt (Error text) ->
                let state = { state with Rows = Resolved([]) }
                state, Cmd.none
        | Msg.KeyDown x ->
            printf "key %s was pressed" x.key
            state, Cmd.none


    let render (state : State) (dispatch : Msg -> unit) =
        // prev a next button a grid, ten pin by asi bylo lepsi mit id, to pak muzu upraivt
        let prev = Html.button [
            prop.text "Prev"
            prop.onClick ( fun _ -> dispatch (LoadPrevRows StartIt) )
        ]
        let next = Html.button [
            prop.text "Next"
            prop.onClick ( fun _ -> dispatch (LoadNextRows StartIt) )
        ]
        let renderrows() =
            List.map Render.gridRow state.DispRows
        let renderempty() = 
            [ for _ in 1..state.Limit - state.DispRows.Length -> Render.gridRowEmpty() ]

        let grid = Render.grid (fun () -> renderrows() @ renderempty() )
        [ Html.p(grid); Html.p([prev; next]) ]
