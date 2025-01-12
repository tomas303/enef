module PgPrices

open Feliz
open Lib
open WgEdit
open WgList

[<ReactComponent>]
let EditPrice (price: Price) onSave onCancel =

    let (value, setValue) = React.useState(price.Value)
    let (fromDate, setFromDate) = React.useState(price.FromDate)
    let (provider_ID, setProvider_ID) = React.useState(price.Provider_ID)
    let (energyKind, setEnergyKind) = React.useState(price.EnergyKind)
    let (providers, setProviders) = React.useState([])

    React.useEffect((fun () -> (
        async {
            let! items = Lib.Api.Providers.loadAll()
            match items with
            | Ok content ->
                let newProviders = content |> List.map (fun x -> x.ID, x.Name)
                setProviders newProviders
            | Error _ ->
                setProviders []
        } |> Async.StartImmediate
        )), [| |])


    let edits = [
        IntField { Name = "value" ; Value = value; HandleChange = setValue }
        StrField { Name = "fromDate" ; Value = fromDate; HandleChange = setFromDate }
        SelectField { Name = "provider_ID" ; Value = provider_ID; Offer = providers; HandleChange = setProvider_ID }
        IntField { Name = "energyKind" ; Value = Constants.EnergyKindToInt.[energyKind]; HandleChange = fun v -> setEnergyKind (Utils.intToEnergyKind v).Value }
        SelectField { Name = "energyKind" ; Value = Constants.EnergyKindToText.[energyKind]; Offer = Seq.toList Constants.TextToEnergyKind.Keys |> List.map (fun key -> key, key); HandleChange = (fun x -> setEnergyKind Constants.TextToEnergyKind.[x]) }
    ]

    let handleSave () = 
        onSave { 
            price with
                Value = value
                FromDate = fromDate
                Provider_ID = provider_ID
                EnergyKind = energyKind }

    WgEdit edits handleSave onCancel


[<ReactComponent>]
let PgPrices() =

    let (providers, setProviders) = React.useState(Map.empty)

    React.useEffect((fun () -> 
        async {
            let! items = Api.Providers.loadAll()
            match items with
            | Ok content -> 
                let newProviders = content |> List.map (fun x -> x.ID, x.Name) |> Map.ofList
                setProviders newProviders
            | Error _ -> 
                setProviders Map.empty
        } |> Async.StartImmediate
    ), [||])

    let memoizedProviders = React.useMemo((fun () -> providers), [| providers |])

    let fetchBefore (price: Price option) count =
        let fromDate =
            match price with
                | Some x -> x.FromDate
                | None -> ""
        Api.Prices.loadPagePrev fromDate count


    let fetchAfter (price: Price option) count =
        let fromDate =
            match price with
                | Some x -> x.FromDate
                | None -> ""
        Api.Prices.loadPageNext fromDate count

    let structure = {
            Headers = [
                { Label = "value" ; FlexBasis = 20; DataGetter = fun (item: Price) -> item.Value.ToString() }
                { Label = "fromDate" ; FlexBasis = 20; DataGetter = fun (item: Price) -> item.FromDate }
                { Label = "provider_ID" ; FlexBasis = 30; DataGetter = fun (item: Price) -> memoizedProviders[item.Provider_ID] }
                { Label = "energyKind" ; FlexBasis = 30; DataGetter = fun (item: Price) -> Constants.EnergyKindToText.[item.EnergyKind] }
            ]
            IdGetter = fun (item: Price) -> item.FromDate
        }

    let props = {|
            Structure = structure
            NewEdit = fun price -> EditPrice price
            ItemNew = fun () -> Utils.newPrice()
            ItemSave = Api.Prices.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
