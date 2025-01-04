module PgEnergies

open Feliz
open Lib
open WgEdit
open WgList

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

    let handleSave () = 
        onSave { 
            energy with
                Amount = amount
                Created = Utils.localDateTimeToUnixTime(created)
                Kind = kind
                Info = info}

    WgEdit edits handleSave onCancel


[<ReactComponent>]
let PgEnergies() =

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

    let rowToListRow (item : Energy) = item.ID, [ 
            Constants.EnergyKindToText.[item.Kind]
            (Utils.unixTimeToLocalDateTime item.Created).ToString("dd.MM.yyyy")
            $"{item.Amount} {Constants.EnergyKindToUnit.[item.Kind]}"
            item.Info 
        ]

    let headers = [
            { Label = "kind" ; FlexBasis = 15 }
            { Label = "created" ; FlexBasis = 25 }
            { Label = "amount" ; FlexBasis = 25 }
            { Label = "info" ; FlexBasis = 100 }
        ]


    let props = {|
            Headers = headers
            RowToListRow = rowToListRow
            NewEdit = fun energy -> EditEnergy energy
            ItemNew = fun () -> Utils.newEnergy()
            ItemSave = Api.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
