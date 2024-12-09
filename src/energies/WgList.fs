namespace Energies

open Feliz
open Elmish

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
    | LoadPrevRows of AsyncOperationEvent<Result<List<Energy>, string>>
    | LoadNextRows of AsyncOperationEvent<Result<List<Energy>, string>>

    let init () : State =
        { 
            Rows = HasNotStartedYet
            DispRows = []
            Limit = 2
        }

    let getNextPin state = 
        if state.DispRows.Length > 0 then state.DispRows.[state.DispRows.Length - 1].Created else 0

    let getPrevPin state = 
        if state.DispRows.Length > 0 then state.DispRows.[0].Created else 0

    let update msg state =
        match msg with
        | Msg.LoadPrevRows x ->
            match x with
            | StartIt ->
                let newpin = getPrevPin state
                let state = { state with Rows = InProgress }
                state, Cmd.fromAsync (Async.map (fun x -> Msg.LoadNextRows(FinishIt(x))) (Api.loadPrevRows newpin state.Limit))
            | FinishIt (Ok items) ->
                let state = 
                    if items.Length > 0
                    then { state with Rows = Resolved(items); DispRows=items } 
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
                state, Cmd.fromAsync (Async.map (fun x -> Msg.LoadNextRows(FinishIt(x))) (Api.loadNextRows newpin state.Limit))
            | FinishIt (Ok items) ->
                let state = 
                    if items.Length > 0
                    then { state with Rows = Resolved(items); DispRows=items } 
                    else { state with Rows = Resolved(items) } 
                state, Cmd.none
            | FinishIt (Error text) ->
                let state = { state with Rows = Resolved([]) }
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
