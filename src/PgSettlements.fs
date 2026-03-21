module PgSettlements

open Feliz
open Lib
open WgEdit
open WgList
open Hooks

let useSettlementEditor (item: Settlement) =
    let date, setDate = React.useState(Utils.unixTimeToLocalDateTime item.Date)
    let energyKind, setEnergyKind = React.useState item.EnergyKind
    let priceType, setPriceType = React.useState item.PriceType
    let amount, setAmount = React.useState item.Amount

    React.useEffect((fun () ->
        setDate (Utils.unixTimeToLocalDateTime item.Date)
        setEnergyKind item.EnergyKind
        setPriceType item.PriceType
        setAmount item.Amount
    ), [| box item |])

    let fields = [
        DateTimeField { Name = "date"; Value = date; HandleChange = setDate }
        SelectField { Name = "energyKind"; Value = Constants.EnergyKindToText.[energyKind]; Offer = Constants.EnergyKindSelection; HandleChange = fun x -> setEnergyKind Constants.TextToEnergyKind.[x] }
        SelectField { Name = "priceType"; Value = Constants.PriceTypeToText.[priceType]; Offer = Constants.PriceTypeSelection; HandleChange = fun x -> setPriceType Constants.TextToPriceType.[x] }
        IntField { Name = "amount"; Value = amount; HandleChange = setAmount }
    ]

    let getUpdatedSettlement () =
        { item with
            Date = Utils.localDateTimeToUnixTime date
            EnergyKind = energyKind
            PriceType = priceType
            Amount = amount }

    fields, getUpdatedSettlement


[<ReactComponent>]
let PgSettlement() =

    let fetchBefore (item: Settlement option) count =
        let fromdate, id =
            match item with
            | Some x -> x.Date, x.ID
            | None -> 0L, ""
        Api.Settlements.loadPagePrev fromdate id count

    let fetchAfter (item: Settlement option) count =
        let fromdate, id =
            match item with
            | Some x -> x.Date, x.ID
            | None -> 0L, ""
        Api.Settlements.loadPageNext fromdate id count

    let structure = {
        Headers = [
            { Label = "date";       FlexBasis = 25; DataGetter = fun (x: Settlement) -> (Utils.unixTimeToLocalDateTime x.Date).ToString("dd.MM.yyyy") }
            { Label = "energyKind"; FlexBasis = 15; DataGetter = fun (x: Settlement) -> Constants.EnergyKindToText.[x.EnergyKind] }
            { Label = "priceType";  FlexBasis = 40; DataGetter = fun (x: Settlement) -> Constants.PriceTypeToText.[x.PriceType] }
            { Label = "amount";     FlexBasis = 20; DataGetter = fun (x: Settlement) -> string x.Amount }
        ]
        IdGetter = fun (x: Settlement) -> x.ID
    }

    let props = {|
        Structure = structure
        useEditor = useSettlementEditor
        ItemNew = Utils.newSettlement
        ItemSave = Api.Settlements.saveItem
        FetchBefore = fetchBefore
        FetchAfter = fetchAfter
    |}

    WgAgenda.WgAgenda props
