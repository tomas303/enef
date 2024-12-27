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
let PgEnergies() =



    let limit = 15
    let (rows, setRows) = React.useState(Deferred.HasNotStartedYet)
    let (displayedRows, setDisplayedRows) = React.useState([])
    let (editState, setEditState) = React.useState(EditState.Browsing)
    let (currentItem, setCurrentItem) = React.useState(Utils.newEnergy())
    let (saveState, setSaveState) = React.useState(Deferred.HasNotStartedYet)


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

    let rowsChanged prevMove rows =
        setRows rows
        match rows with
        | Deferred.Resolved content ->
            match content with
            | Ok items ->
                match List.length items with
                | len when len = limit ->
                    setDisplayedRows(items)
                | len when len > 0 && prevMove ->
                    let last = (items.[len-1].Created, items.[len-1].ID)
                    let addItems = List.filter (fun x -> (x.Created, x.ID) > last ) displayedRows
                    let newDispRows = List.truncate limit (List.append items addItems)
                    setDisplayedRows(newDispRows)
                | len when len > 0 && not prevMove ->
                    setDisplayedRows(items)
                | _ -> ()
            | _ -> ()
        | _ -> ()

    let loadingInProgress = rows = Deferred.InProgress

    let handlePrevPage =
        let created, id = getTopCreatedId ()
        React.useDeferredCallback((fun () -> Api.loadPagePrev created id limit false), rowsChanged true)

    let handlePrevRow =
        let created, id = getBottomCreatedId ()
        let apicall () = 
            if displayedRows.Length < limit 
            then Api.loadPagePrev created id (displayedRows.Length + 1) true
            else Api.loadPagePrev created id limit false
        React.useDeferredCallback((fun () -> apicall ()), rowsChanged true)

    let handleNextPage =
        let created, id = getBottomCreatedId ()
        React.useDeferredCallback((fun () -> Api.loadPageNext created id limit false), rowsChanged false)

    let handleNextRow =
        let created, id = getTopCreatedId ()
        React.useDeferredCallback((fun () -> Api.loadPageNext created id limit false), rowsChanged false)

    let handleRefresh =
        React.useDeferredCallback((fun energy -> Api.loadPagePrev energy.Created energy.ID limit true), rowsChanged false)

    let handleAdd () =
        let currentItem = Utils.newEnergy()
        setEditState(EditState.Adding)

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
                            setCurrentItem energy
                            handleRefresh energy
                        | Error _ -> ()
        

            )
        )
    
    let handleCancel () =
        setEditState(EditState.Browsing)

    React.useEffect((fun () -> (
        let call =
            async {
                let! result = Api.loadPageNext 0 "" limit false
                rowsChanged false (Deferred.Resolved result)
            }
        Async.StartImmediate call
        )), [|  |])
    

    let dataRow (item : Energy) = item.ID, [ 
            Constants.EnergyKindToText.[item.Kind]
            (Utils.unixTimeToLocalDateTime item.Created).ToString("dd.MM.yyyy HH:mm")
            $"{item.Amount} {Constants.EnergyKindToUnit.[item.Kind]}"
            item.Info 
        ]

    let headers = [
            { Label = "kind" ; FlexBasis = 10 }
            { Label = "created" ; FlexBasis = 30 }
            { Label = "amount" ; FlexBasis = 15 }
            { Label = "info" ; FlexBasis = 100 }
        ]

    let rows = List.map dataRow displayedRows


    Html.div [
        WgList headers rows loadingInProgress limit handlePrevPage handleNextPage handlePrevRow handleNextRow handleAdd
        match saveState with
        | Deferred.HasNotStartedYet -> ()
        | Deferred.InProgress ->  Html.text "saving ing progress"
        | Deferred.Failed error ->  Html.text $"error during save {error.Message}"
        | Deferred.Resolved content ->
            match content with
                | Ok energy -> ()
                | Error text -> Html.text $"error during processing {text}"

        if editState = EditState.Adding then EditEnergy currentItem handleSave handleCancel else ()
    ]
