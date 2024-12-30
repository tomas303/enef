module PgEnergies

open Feliz
open Feliz.UseDeferred
open Lib
open WgList
open WgEdit


[<ReactComponent>]
let EditEnergy energy onSave onCancel =

    let (kind, setKind) = React.useState(energy.Kind)
    let (created, setCreated) = React.useState(Utils.unixTimeToLocalDateTime(energy.Created))
    let (amount, setAmount) = React.useState(energy.Amount)
    let (info, setInfo) = React.useState(energy.Info)

    let edits = [
        DateTimeField { Name = "created" ; Value = created; HandleChange = setCreated }
        SelectField { Name = "kind" ; Value = Constants.EnergyKindToText.[kind]; Offer = Seq.toList Constants.TextToEnergyKind.Keys; HandleChange = (fun x -> setKind Constants.TextToEnergyKind.[x]) }
        IntField { Name = "amount" ; Value = amount; HandleChange = setAmount }
        StrField { Name = "info" ; Value = info; HandleChange = setInfo }
    ]

    Html.div [
        WgEdit edits
        Html.div [
            Html.button [
                prop.text "Cancel"
                prop.onClick (fun _ -> onCancel())
            ]
            Html.button [
                prop.text "Save"
                prop.onClick (fun _ -> 
                    let x = 
                        { energy with
                            Amount = amount
                            Created = Utils.localDateTimeToUnixTime(created)
                            Kind = kind
                            Info = info
                        }
                    onSave x
                )
            ]
        ]
    ]

[<RequireQualifiedAccess>]
type EditState =
    | Browsing
    | Adding
    | Editing
    | Saving

[<ReactComponent>]
let Energies() =

    let limit = 15
    let (rows, setRows) = React.useState(Deferred.HasNotStartedYet)
    let (displayedRows, setDisplayedRows) = React.useState([])
    let (editState, setEditState) = React.useState(EditState.Browsing)
    let (saveState, setSaveState) = React.useState(Deferred.HasNotStartedYet)
    let (cursor, setCursor) = React.useState(-1)


    // System.Console.WriteLine $"NEXT ROUND"
    // System.Console.WriteLine $"cursor = {cursor}"
    // // System.Console.WriteLine $"displayedRows = {displayedRows}"
    // System.Console.WriteLine $"rows = {rows}"
    // System.Console.WriteLine $"editState = {editState}"
    // System.Console.WriteLine $"saveState = {saveState}"


    let isCursorValid () =
        cursor >= 0 && cursor <= displayedRows.Length - 1


    let getBottomCreatedId () = 
        if displayedRows.Length > 0 then 
            let energy = displayedRows.[displayedRows.Length - 1]
            (energy.Created, energy.ID)
        else (0, "")


    let getTopCreatedId () = 
        if displayedRows.Length > 0 then
            let energy = displayedRows.[0]
            (energy.Created, energy.ID)
        else (0, "")

    let changeDispRows rows =
        setDisplayedRows(rows)
        match cursor with
        | _ when rows.Length = 0 -> setCursor(-1)
        | x when x > rows.Length - 1 -> setCursor(rows.Length - 1)
        | x when x < 0 -> setCursor(0)
        | _ -> ()


    let rowsChanged upDirection rows =
        setRows rows
        match rows with
        | Deferred.HasNotStartedYet -> setRows Deferred.HasNotStartedYet
        | Deferred.InProgress -> setRows Deferred.InProgress
        | Deferred.Failed error -> setRows (Deferred.Failed error)
        | Deferred.Resolved (Ok content) ->
            setRows (Deferred.Resolved (Ok content))
            match List.length content with
            | len when len = limit ->
                changeDispRows content
            | len when len > 0 && upDirection ->
                let last = (content.[len-1].Created, content.[len-1].ID)
                let addItems = List.filter (fun x -> (x.Created, x.ID) > last ) displayedRows
                let newDispRows = List.truncate limit (List.append content addItems)
                changeDispRows newDispRows
            | len when len > 0 && not upDirection ->
                changeDispRows content
            | _ -> ()
        | Deferred.Resolved (Error text) -> 
            setRows (Deferred.Resolved (Error text))


    let handlePageUp =
        let created, id = getTopCreatedId ()
        React.useDeferredCallback((fun () -> Api.loadPagePrev created id limit false), rowsChanged true)


    let handlePageDown =
        let created, id = getBottomCreatedId ()
        React.useDeferredCallback((fun () -> Api.loadPageNext created id limit false), rowsChanged false)


    let handleRowUp =

        let scroll =
            let created, id = getBottomCreatedId ()
            let apicall = 
                if displayedRows.Length < limit 
                then Api.loadPagePrev created id (displayedRows.Length + 1) true
                else Api.loadPagePrev created id limit false
            React.useDeferredCallback((fun () -> apicall), rowsChanged true)

        if cursor > 0 
        then fun () -> setCursor(cursor - 1) 
        else scroll


    let handleRowDown =

        let scroll =
            let created, id = getTopCreatedId ()
            React.useDeferredCallback((fun () -> Api.loadPageNext created id limit false), rowsChanged false)

        if cursor < displayedRows.Length - 1 
        then fun () -> setCursor(cursor + 1) 
        else scroll


    let handleRefresh =

        let abovePart created id limit = 
            async {
                if limit > 0 then
                    let! rows = Api.loadPagePrev created id limit false
                    return rows
                else
                    return Result.Ok []
            }
        
        let bellowPart created id limit = 
            async {
                if limit > 0 then
                    let! rows = Api.loadPageNext created id limit false
                    return rows
                else
                    return Result.Ok []
            }

        let refresh energy = 
            async {
                let above = abovePart energy.Created energy.ID cursor
                let bellow = bellowPart energy.Created energy.ID limit
                let! results = [ above; bellow ] |> Async.Parallel
                return energy, results.[0], results.[1]
            }


        let onSet x =
            match x with
            | Deferred.HasNotStartedYet -> setRows Deferred.HasNotStartedYet
            | Deferred.InProgress -> setRows Deferred.InProgress
            | Deferred.Failed error -> setRows (Deferred.Failed error)
            | Deferred.Resolved (energy, above, bellow) -> 
                match above, bellow with
                | Ok above, Ok bellow ->
                    let newDispRows = List.truncate limit (above @ [energy] @ bellow)
                    let newCursor = List.findIndex (fun x -> x.ID = energy.ID) newDispRows
                    setRows (Deferred.Resolved (Ok newDispRows))
                    setDisplayedRows newDispRows
                    setCursor newCursor
                | Error above, Error bellow ->
                    setRows (Deferred.Resolved (Error (above + bellow)))
                | Error above, Ok _ ->
                    setRows (Deferred.Resolved (Error above))
                | Ok _, Error bellow ->
                    setRows (Deferred.Resolved (Error bellow))


        React.useDeferredCallback(refresh, onSet)


    let handleAdd () =
        if editState = EditState.Browsing then setEditState(EditState.Adding)


    let handleEdit () =
        if editState = EditState.Browsing && isCursorValid() then setEditState(EditState.Editing)


    let handleSave =
        React.useDeferredCallback(Api.saveItem, 
            (fun x -> 
                setSaveState x
                match x with
                | Deferred.HasNotStartedYet -> setEditState(EditState.Browsing)
                | Deferred.InProgress -> setEditState(EditState.Saving)
                | Deferred.Failed error -> setEditState(EditState.Browsing)
                | Deferred.Resolved content ->
                    match content with
                        | Ok energy ->
                            match editState with
                            | EditState.Adding ->
                                handleRefresh energy
                            | EditState.Editing ->
                                handleRefresh energy
                            | _ -> ()
                        | Error _ -> ()
                    setEditState(EditState.Browsing)
            )
        )
    
    let handleCancel () =
        setEditState(EditState.Browsing)


    let renderError x = 
        match x with
        | Deferred.HasNotStartedYet -> Html.none
        | Deferred.InProgress -> Html.none
        | Deferred.Failed error -> Html.text error.Message
        | Deferred.Resolved content ->
            match content with
            | Ok _ -> Html.none
            | Error (text: string) -> Html.text text


    let dataRow (item : Energy) = item.ID, [ 
            Constants.EnergyKindToText.[item.Kind]
            (Utils.unixTimeToLocalDateTime item.Created).ToString("dd.MM.yyyy HH:mm")
            $"{item.Amount} {Constants.EnergyKindToUnit.[item.Kind]}"
            item.Info 
        ]


    let firstLoad() =
        React.useEffect((fun () -> (
            let call =
                async {
                    let! result = Api.loadPageNext 0 "" limit false
                    rowsChanged false (Deferred.Resolved result)
                }
            Async.StartImmediate call
            )), [||])


    firstLoad()

    let headers = [
            { Label = "kind" ; FlexBasis = 10 }
            { Label = "created" ; FlexBasis = 40 }
            { Label = "amount" ; FlexBasis = 15 }
            { Label = "info" ; FlexBasis = 100 }
        ]

    let dataRows = List.map dataRow displayedRows

    let props = {|
            Headers = headers
            Rows = dataRows
            LoadingInProgress = rows = Deferred.InProgress
            RowCount = limit
            Cursor = cursor
            OnPageUp = handlePageUp
            OnPageDown = handlePageDown
            OnRowUp = handleRowUp
            OnRowDown = handleRowDown
            OnAdd = handleAdd
            OnEdit = handleEdit
        |}

    let renderEdit =
        match editState with
        | EditState.Adding -> EditEnergy (Utils.newEnergy()) handleSave handleCancel 
        | EditState.Editing -> 
            if isCursorValid()
            then EditEnergy (displayedRows.[cursor]) handleSave handleCancel
            else Html.text $"invalid cursor: {cursor}"

        | _ -> Html.none

    Html.div [
        renderError rows
        renderError saveState
        WgList props
        renderEdit
    ]


[<ReactComponent>]
let PgEnergies() =
    Html.div [
            Energies()
        ]