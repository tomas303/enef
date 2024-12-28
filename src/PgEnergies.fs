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
    | Saving

[<ReactComponent>]
let Energies() =

    let limit = 15
    let (rows, setRows) = React.useState(Deferred.HasNotStartedYet)
    let (displayedRows, setDisplayedRows) = React.useState([])
    let (editState, setEditState) = React.useState(EditState.Browsing)
    let (saveState, setSaveState) = React.useState(Deferred.HasNotStartedYet)
    let (cursor, setCursor) = React.useState(-1)


    let isCursorValid () =
        // System.Console.WriteLine $"MMMcursor: {cursor}"
        // System.Console.WriteLine $"MMMlength: {displayedRows.Length}"
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
        System.Console.WriteLine $"cursor from disprows: {cursor}"
        match cursor with
        | _ when rows.Length = 0 -> setCursor(-1)
        | x when x > rows.Length - 1 -> setCursor(rows.Length - 1)
        | x when x < 0 -> setCursor(0)
        | _ -> ()

    let rowsChanged upDirection rows =
        setRows rows
        System.Console.WriteLine $"cursor from rowschanged: {cursor}"
        match rows with
        | Deferred.Resolved content ->
            match content with
            | Ok items ->
                match List.length items with
                | len when len = limit ->
                    changeDispRows items
                | len when len > 0 && upDirection ->
                    let last = (items.[len-1].Created, items.[len-1].ID)
                    let addItems = List.filter (fun x -> (x.Created, x.ID) > last ) displayedRows
                    let newDispRows = List.truncate limit (List.append items addItems)
                    changeDispRows newDispRows
                | len when len > 0 && not upDirection ->
                    changeDispRows items
                | _ -> ()
            | _ -> ()
        | _ -> ()

    let scrollUp =
        let created, id = getBottomCreatedId ()
        let apicall () = 
            if displayedRows.Length < limit 
            then Api.loadPagePrev created id (displayedRows.Length + 1) true
            else Api.loadPagePrev created id limit false
        React.useDeferredCallback((fun () -> apicall ()), rowsChanged true)

    let scrollDown =
        let created, id = getTopCreatedId ()
        React.useDeferredCallback((fun () -> Api.loadPageNext created id limit false), rowsChanged false)

    let handlePageUp =
        let created, id = getTopCreatedId ()
        React.useDeferredCallback((fun () -> Api.loadPagePrev created id limit false), rowsChanged true)

    let handlePageDown =
        let created, id = getBottomCreatedId ()
        React.useDeferredCallback((fun () -> Api.loadPageNext created id limit false), rowsChanged false)

    let handleRowUp () =
        if cursor > 0 
        then setCursor(cursor - 1) 
        else scrollUp()

    let handleRowDown () =
        if cursor < displayedRows.Length - 1 
        then setCursor(cursor + 1) 
        else scrollDown()

    let handleRefresh =

        let refresh energy = 
            async {
                let! rows = Api.loadPagePrev energy.Created energy.ID limit true
                return energy.ID, rows
            }

        let onSet x =
            match x with
            | Deferred.HasNotStartedYet -> rowsChanged false Deferred.HasNotStartedYet
            | Deferred.InProgress -> rowsChanged false Deferred.InProgress
            | Deferred.Failed error -> rowsChanged false (Deferred.Failed error)
            | Deferred.Resolved (id, content) -> 
                rowsChanged false (Deferred.Resolved content)
                match content with
                | Ok rows ->
                    let newCursor = List.tryFindIndex (fun x -> x.ID = id) rows
                    System.Console.WriteLine $"found newcursor {newCursor}"
                    Option.iter (fun x -> setCursor x) newCursor
                | _ -> ()


        React.useDeferredCallback(refresh, onSet)


    let handleAdd () =
        if isCursorValid() 
        then System.Console.WriteLine "is valid"
        else System.Console.WriteLine "isnt valid"
        if isCursorValid() then setEditState(EditState.Adding)

    let handleSave =
        React.useDeferredCallback(Api.saveItem, 
            (fun x -> 
                setSaveState x
                match x with
                | Deferred.HasNotStartedYet -> setEditState(EditState.Browsing)
                | Deferred.InProgress -> setEditState(EditState.Saving)
                | Deferred.Failed error -> setEditState(EditState.Browsing)
                | Deferred.Resolved content ->
                    setEditState(EditState.Browsing)
                    match content with
                        | Ok energy ->
                            handleRefresh energy
                        | Error _ -> ()
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
        |}

    let renderEdit = 
        if editState = EditState.Adding then
            if isCursorValid() then
                // EditEnergy displayedRows.[cursor] handleSave handleCancel 
                EditEnergy (Utils.newEnergy()) handleSave handleCancel 
            else
                Html.none
        else 
            Html.none

    System.Console.WriteLine $"cursor: {cursor}"
    System.Console.WriteLine $"length: {displayedRows.Length}"

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