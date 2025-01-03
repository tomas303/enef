module PgEnergies

open Feliz
open Feliz.UseDeferred
open Lib
open WgList
open WgEdit

module GridBuffer =

    type Data<'T> = {
        Top: int
        Bottom: int
        Cursor: int
        ViewSize: int
        DataSize: int
        Data: List<'T>
        Lasterror: Option<string>
    }   

    // type Fetch<'T> = 'T option -> int -> Async<List<'T>>

    let applyDelta data delta = 
        let newCursor = data.Cursor + delta
        if newCursor < data.Top || newCursor > data.Bottom then
            let newTop = data.Top + delta
            {data with 
                Cursor = newCursor
                Top = newTop
                Bottom = newTop + data.ViewSize - 1
            }
        else { data with Cursor = newCursor }

    let readBefore data fetchBefore = async {
        let delta = abs(data.Top)
        let! fetchData =
            if data.Data.Length > 0 
            then fetchBefore (Some data.Data[0]) delta
            else fetchBefore None delta
        match fetchData with
        | Ok content ->
            let newData = content @ data.Data
            let newData = 
                if newData.Length > data.DataSize then
                    List.take data.DataSize newData
                else
                    newData
            return 
                { data with
                    Cursor = data.Cursor + content.Length
                    Top = data.Top + content.Length
                    Bottom = data.Bottom + content.Length
                    Data = newData }
        | Error error ->
            return { data with Lasterror = Some(error) }
        }

    let readAfter data fetchAfter = async {
        let delta = max data.ViewSize (abs(data.Bottom - data.Data.Length))
        let! fetchData = 
            if data.Data.Length > 0 
            then fetchAfter (Some data.Data[data.Data.Length - 1]) delta
            else fetchAfter None delta
        match fetchData with
        | Ok content ->
            let newData = data.Data @ content
            let removeCnt =
                if newData.Length > data.DataSize
                then newData.Length - data.DataSize
                else 0
            return 
                { data with
                    Cursor = data.Cursor - removeCnt
                    Top = data.Top - removeCnt
                    Bottom = data.Bottom - removeCnt
                    Data = List.skip removeCnt newData }
        | Error error ->
            return { data with Lasterror = Some(error) }
        }

    let correct data =
        let newTop =
            if data.Top < 0 then
                0
            else if data.Top > data.Data.Length - 1 then
                data.Data.Length - 1
            else
                data.Top
        let newBottom = 
            if newTop + data.ViewSize - 1 > data.Data.Length - 1
            then data.Data.Length - 1
            else newTop + data.ViewSize - 1
        let newCursor =
            if data.Cursor < newTop 
            then newTop
            else if data.Cursor > newBottom
            then newBottom
            else data.Cursor
        { data with
            Cursor = newCursor
            Top = newTop
            Bottom = newBottom }

    let move data delta fetchBefore fetchAfter = async {
        Dbg.wl $"before applydelta {data}"
        let newData = applyDelta data delta
        Dbg.wl $"after applydelta {newData}"
        let! newData =
            if newData.Top < 0 then
                readBefore newData fetchBefore
            else if newData.Bottom > newData.Data.Length - 1 then
                readAfter newData fetchAfter
            else
                async { return newData }
        Dbg.wl $"before correct {newData}"
        let newData = correct newData
        Dbg.wl $"after correct {newData}"
        return newData
    }

    let view data =
        data.Data[data.Top..data.Bottom]

    let create viewSize dataSize = {
            Top = -1
            Bottom = -1
            Cursor = -1
            ViewSize = viewSize
            DataSize = dataSize
            Data = []
            Lasterror = None
        }   

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

    let frameHeight = 15
    let bufferLimit = 30

    let (rows, setRows) = React.useState(Deferred.HasNotStartedYet)
    let (displayedRows, setDisplayedRows) = React.useState([])
    let (editState, setEditState) = React.useState(EditState.Browsing)
    let (saveState, setSaveState) = React.useState(Deferred.HasNotStartedYet)
    let (cursor, setCursor) = React.useState(-1)

    let (buffer, setBuffer) = React.useState(GridBuffer.create 15 100)
    let view = GridBuffer.view buffer
    let (deltaMove, setDeltaMove) = React.useState(1)
    
    

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


    let fetchBefore energy count =
        let created, id =
            match energy with
                | Some x -> x.Created, x.ID
                | None -> 0, ""
        Api.loadPagePrev created id count false

    let fetchAfter energy count =
        let created, id =
            match energy with
                | Some x -> x.Created, x.ID
                | None -> 0, ""
        Api.loadPageNext created id count false


    React.useEffect((fun () -> (
        async {
            if deltaMove <> 0 then
                Dbg.wl $"deltamove {deltaMove}"
                let! newBuffer = GridBuffer.move buffer deltaMove fetchBefore fetchAfter
                setBuffer newBuffer
                setDeltaMove 0
        } |> Async.StartImmediate
        )), [| box deltaMove |])


    // let handlePageUp() =
    //     // let created, id = getTopCreatedId ()
    //     // React.useDeferredCallback((fun () -> Api.loadPagePrev created id limit false), rowsChanged true)
    //     setCursor(cursor - limit)

    // let handlePageDown() =
    //     // let created, id = getBottomCreatedId ()
    //     // React.useDeferredCallback((fun () -> Api.loadPageNext created id limit false), rowsChanged false)
    //     setCursor(cursor + limit)

    // let handleRowUp() =

    //     // let scroll =
    //     //     let created, id = getBottomCreatedId ()
    //     //     let apicall = 
    //     //         if displayedRows.Length < limit 
    //     //         then Api.loadPagePrev created id (displayedRows.Length + 1) true
    //     //         else Api.loadPagePrev created id limit false
    //     //     React.useDeferredCallback((fun () -> apicall), rowsChanged true)

    //     // if cursor > 0 
    //     // then fun () -> setCursor(cursor - 1) 
    //     // else scroll
    //     setCursor(cursor - 1)


    // let handleRowDown() =

    //     // let scroll =
    //     //     let created, id = getTopCreatedId ()
    //     //     React.useDeferredCallback((fun () -> Api.loadPageNext created id limit false), rowsChanged false)

    //     // if cursor < displayedRows.Length - 1 
    //     // then fun () -> setCursor(cursor + 1) 
    //     // else scroll
    //     setCursor(cursor + 1)


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

    // let dataRows = List.map dataRow displayedRows
    let dataRows = List.map dataRow view

    let props = {|
            Headers = headers
            Rows = dataRows
            LoadingInProgress = rows = Deferred.InProgress
            RowCount = buffer.ViewSize
            Cursor = buffer.Cursor - buffer.Top
            OnPageUp = fun () -> setDeltaMove(buffer.ViewSize * -1)
            OnPageDown = fun () -> setDeltaMove(buffer.ViewSize)
            OnRowUp = fun () -> setDeltaMove(-1)
            OnRowDown = fun () -> setDeltaMove(1)
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