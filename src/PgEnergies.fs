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

    let structure = {
            Headers = [
                { Label = "kind" ; FlexBasis = 15; DataGetter = fun item -> Constants.EnergyKindToText.[item.Kind] }
                { Label = "created" ; FlexBasis = 25; DataGetter = fun item -> (Utils.unixTimeToLocalDateTime item.Created).ToString("dd.MM.yyyy") }
                { Label = "amount" ; FlexBasis = 25; DataGetter = fun item -> $"{item.Amount} {Constants.EnergyKindToUnit.[item.Kind]}" }
                { Label = "info" ; FlexBasis = 100; DataGetter = fun item -> item.Info }
            ]
            IdGetter = fun item -> item.ID
        }

    let props = {|
            Structure = structure
            NewEdit = fun energy -> EditEnergy energy
            ItemNew = fun () -> Utils.newEnergy()
            ItemSave = Api.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
