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
    | LoadFirstRows
    | LoadPrevRows of AsyncOperationEvent<Result<List<Energy>, string>>
    | LoadNextRows of AsyncOperationEvent<Result<List<Energy>, string>>
    | Refresh of Energy
    | KeyDown of Browser.Types.KeyboardEvent

    let getBottomCreatedId state = 
        if state.DispRows.Length > 0 then 
            let energy = state.DispRows.[state.DispRows.Length - 1]
            (energy.Created, energy.ID)
        else (0, "")

    let getTopCreatedId state = 
        if state.DispRows.Length > 0 then
            let energy = state.DispRows.[0]
            (energy.Created, energy.ID)
        else (0, "")

    let getNextPageCmd created id limit =
        Cmd.fromAsync (Async.map (fun x -> Msg.LoadNextRows(FinishIt(x))) (Api.loadPageNext created id limit false))

    let getPrevPageCmd created id limit =
        Cmd.fromAsync (Async.map (fun x -> Msg.LoadPrevRows(FinishIt(x))) (Api.loadPagePrev created id limit false))

    let getNextPageIncludeCmd created id limit =
        Cmd.fromAsync (Async.map (fun x -> Msg.LoadNextRows(FinishIt(x))) (Api.loadPageNext created id limit true))

    let getPrevPageIncludeCmd created id limit =
        Cmd.fromAsync (Async.map (fun x -> Msg.LoadPrevRows(FinishIt(x))) (Api.loadPagePrev created id limit true))

    let init () =
        let limit = 15
        { 
            Rows = HasNotStartedYet
            DispRows = []
            Limit = limit
        }

    let interceptKey key =
        key = "PageUp" 
        || key = "PageDown"
        || key = "ArrowUp"
        || key = "ArrowDown"

    let keydown onKeydown =
        let run dispatch =
            let handler (event: Event) = 
                let kev = event :?> KeyboardEvent
                printf "key %s was pressed" kev.key
                if interceptKey kev.key then
                    kev.preventDefault()
                    dispatch (onKeydown (kev))
            document.addEventListener ("keydown", handler)
            { new IDisposable with
                member _.Dispose() = document.removeEventListener ("keydown", handler) }
        run

    let subscribe isactive state =
        if isactive then
            [ ["keydown"], keydown Msg.KeyDown ]
        else
            []

    let update msg state =
        match msg with
        | Msg.LoadFirstRows ->
            let state = { state with Rows = InProgress }
            state, getNextPageCmd 0 "" state.Limit 
        | Msg.LoadPrevRows x ->
            match x with
            | StartIt ->
                let created, id = getTopCreatedId state
                let state = { state with Rows = InProgress }
                state, getPrevPageCmd created id state.Limit
            | FinishIt (Ok items) ->
                match items.Length with
                | len when len = state.Limit ->
                    { state with Rows = Resolved(items); DispRows=items }, Cmd.none
                | len when len > 0 ->
                    let last = (items.[len-1].Created, items.[len-1].ID)
                    let addItems = List.filter (fun x -> (x.Created, x.ID) > last ) state.DispRows
                    let newDispRows= List.truncate state.Limit (List.append items addItems)
                    { state with Rows = Resolved(items); DispRows=newDispRows }, Cmd.none
                | _ ->
                    state, Cmd.none
            | FinishIt (Error text) ->
                let state = { state with Rows = Resolved([]) }
                state, Cmd.none
        | Msg.LoadNextRows x ->
            match x with
            | StartIt ->
                let created, id = getBottomCreatedId state
                let state = { state with Rows = InProgress }
                state, getNextPageCmd created id state.Limit
            | FinishIt (Ok items) ->
                let state = 
                    if items.Length > 0
                    then { state with Rows = Resolved(items); DispRows=items } 
                    else { state with Rows = Resolved(items) } 
                state, Cmd.none
            | FinishIt (Error text) ->
                let state = { state with Rows = Resolved([]) }
                state, Cmd.none
        | Msg.Refresh x ->
            let state = { state with DispRows = [];  Rows = InProgress }
            state, getPrevPageIncludeCmd x.Created x.ID state.Limit
        | Msg.KeyDown x ->
            match x.key with
            | "PageUp" -> state, Cmd.ofMsg (Msg.LoadPrevRows StartIt)
            | "PageDown" -> state, Cmd.ofMsg (Msg.LoadNextRows StartIt)
            | "ArrowUp" -> //state, Cmd.ofMsg (Msg.LoadPrevRows StartIt)
                let created, id = getBottomCreatedId state
                let state = { state with Rows = InProgress }
                if state.DispRows.Length < state.Limit 
                then { state with DispRows = [] }, getPrevPageIncludeCmd created id (state.DispRows.Length + 1)
                else state, getPrevPageCmd created id state.Limit
            | "ArrowDown" -> //state, Cmd.ofMsg (Msg.LoadNextRows StartIt)
                let created, id = getTopCreatedId state
                let state = { state with Rows = InProgress }
                state, getNextPageCmd created id state.Limit
            | _ -> state, Cmd.none


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
