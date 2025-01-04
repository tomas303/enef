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
        let newData = applyDelta data delta
        let! newData =
            if newData.Top < 0 then
                readBefore newData fetchBefore
            else if newData.Bottom > newData.Data.Length - 1 then
                readAfter newData fetchAfter
            else
                async { return newData }
        let newData = correct newData
        return newData
    }

    let view data =
        data.Data[data.Top..data.Bottom]

    let cursorValid data =
        data.Cursor >= 0 && data.Cursor <= data.Data.Length - 1

    let recordUpdate data item =
        let newData = data.Data[0 .. data.Cursor - 1] @ [item] @ data.Data[data.Cursor + 1 .. data.Data.Length - 1]
        { data with Data = newData }

    let recordInsert data item =
        let newData = data.Data[0 .. data.Cursor - 1] @ [item] @ data.Data[data.Cursor .. data.Data.Length - 1]
        { data with Data = newData }

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
type State =
    | Browsing
    | Adding
    | Editing
    | Saving
    | Shifting

[<ReactComponent>]
let Energies() =


    let (buffer, setBuffer) = React.useState(GridBuffer.create 15 100)
    let view = GridBuffer.view buffer
    let (deltaMove, setDeltaMove) = React.useState(1)
    let (state, setState) = React.useState(State.Browsing)
    let (lastError, setLastError) = React.useState(None)

    let fetchBefore energy count =
        let created, id =
            match energy with
                | Some x -> x.Created, x.ID
                | None -> 0, ""
        Api.loadPagePrev created id count


    let fetchAfter energy count =
        let created, id =
            match energy with
                | Some x -> x.Created, x.ID
                | None -> 0, ""
        Api.loadPageNext created id count


    React.useEffect((fun () -> (
        async {
            if state = State.Browsing && deltaMove <> 0 then
                let! newBuffer = GridBuffer.move buffer deltaMove fetchBefore fetchAfter
                setBuffer newBuffer
                setDeltaMove 0
                setLastError newBuffer.Lasterror
        } |> Async.StartImmediate
        )), [| box deltaMove |])


    let handleSave =
        React.useDeferredCallback(Api.saveItem, 
            (fun x ->
                setLastError None
                match x with
                | Deferred.HasNotStartedYet -> setState State.Browsing
                | Deferred.InProgress -> setState State.Saving
                | Deferred.Failed exn -> 
                    setState State.Browsing
                    setLastError (Some exn.Message)
                | Deferred.Resolved content ->
                    match content with
                        | Ok energy ->
                            match state with
                            | State.Adding ->
                                let newBuffer = GridBuffer.recordInsert buffer energy
                                setBuffer newBuffer
                            | State.Editing ->
                                let newBuffer = GridBuffer.recordUpdate buffer energy
                                setBuffer newBuffer
                            | _ -> ()
                        | Error error -> 
                            setLastError (Some error)
                    setState(State.Browsing)
            )
        )


    let handleCancel () =
        setState(State.Browsing)


    let renderError () =
        match lastError with
        | Some error -> Html.text error
        | None -> Html.none

    let dataRow (item : Energy) = item.ID, [ 
            Constants.EnergyKindToText.[item.Kind]
            (Utils.unixTimeToLocalDateTime item.Created).ToString("dd.MM.yyyy HH:mm")
            $"{item.Amount} {Constants.EnergyKindToUnit.[item.Kind]}"
            item.Info 
        ]


    let headers = [
            { Label = "kind" ; FlexBasis = 10 }
            { Label = "created" ; FlexBasis = 40 }
            { Label = "amount" ; FlexBasis = 15 }
            { Label = "info" ; FlexBasis = 100 }
        ]

    let dataRows = List.map dataRow view

    let props = {|
            Headers = headers
            Rows = dataRows
            IsBrowsing = state = State.Browsing
            RowCount = buffer.ViewSize
            Cursor = buffer.Cursor - buffer.Top
            OnPageUp = fun () -> if state = State.Browsing then setDeltaMove(buffer.ViewSize * -1)
            OnPageDown = fun () -> if state = State.Browsing then setDeltaMove(buffer.ViewSize)
            OnRowUp = fun () -> if state = State.Browsing then setDeltaMove(-1)
            OnRowDown = fun () -> if state = State.Browsing then setDeltaMove(1)
            OnAdd = fun () -> if state = State.Browsing then setState(State.Adding)
            OnEdit = fun () -> if state = State.Browsing && GridBuffer.cursorValid buffer then setState(State.Editing)
        |}

    let renderEdit =
        match state with
        | State.Adding -> EditEnergy (Utils.newEnergy()) handleSave handleCancel 
        | State.Editing -> 
            if GridBuffer.cursorValid buffer
            then EditEnergy (buffer.Data[buffer.Cursor]) handleSave handleCancel
            else Html.text $"invalid cursor: {buffer.Cursor}"

        | _ -> Html.none

    Html.div [
        renderError ()
        WgList props
        renderEdit
    ]


[<ReactComponent>]
let PgEnergies() =
    Html.div [
            Energies()
        ]