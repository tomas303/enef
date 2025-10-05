module PgPrices

open Feliz
open Lib
open WgEdit
open WgList

let usePriceEditor (price: Price) =
    let energyKind, setEnergyKind = React.useState(price.EnergyKind)
    let priceType, setPriceType = React.useState(price.PriceType)
    let value, setValue = React.useState(price.Value)
    let provider_id, setProvider_id = React.useState(price.Provider_ID)
    let providers, setProviders = React.useState([])
    let name, setName = React.useState(price.Name)

    React.useEffect((fun () ->
        setEnergyKind(price.EnergyKind)
        setPriceType(price.PriceType)
        setName(price.Name)
        setProvider_id(price.Provider_ID)
        setValue(price.Value)
    ), [| box price |])  // ← Depend on entire energy object


    React.useEffectOnce(fun () -> 
        async {
            let! items = Lib.Api.Providers.loadAll()
            match items with
            | Ok content ->
                let newProviders = content |> List.map (fun x -> x.ID, x.Name)
                setProviders newProviders
            | Error _ ->
                setProviders []
        } |> Async.StartImmediate
    )

    let fields = [
        SelectField { Name = "energyKind" ; Value = Constants.EnergyKindToText[energyKind]; Offer = Constants.EnergyKindSelection; HandleChange = (fun x -> setEnergyKind Constants.TextToEnergyKind[x]) }
        SelectField { Name = "priceType" ; Value = Constants.PriceTypeToText[priceType]; Offer = Constants.PriceTypeSelection; HandleChange = (fun x -> setPriceType Constants.TextToPriceType[x]) }
        IntField { Name = "value" ; Value = value; HandleChange = setValue }
        SelectField { Name = "provider_id" ; Value = provider_id; Offer = providers; HandleChange = setProvider_id }
        StrField { Name = "name" ; Value = name; HandleChange = setName }
    ]

    let getUpdatedPrice () = 
        { price with
            EnergyKind = energyKind 
            PriceType = priceType
            Value = value
            Provider_ID = provider_id
            Name = name }

    fields, getUpdatedPrice


[<ReactComponent>]
let PgPrices() =

    let (providers, setProviders) = React.useState(Map.empty)

    React.useEffectOnce(fun () -> 
        async {
            let! items = Api.Providers.loadAll()
            match items with
            | Ok content -> 
                Dbg.wl $"providers: {content}"
                let newProducts = content |> List.map (fun x -> x.ID, x.Name) |> Map.ofList
                setProviders newProducts
            | Error _ -> 
                setProviders Map.empty
        } |> Async.StartImmediate
    )

    let memoizedProviders = React.useMemo((fun () -> providers), [| providers |])

    let fetchBefore (price: Price option) count =
        let name, id =
            match price with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Prices.loadPagePrev name id count


    let fetchAfter (price: Price option) count =
        let name, id =
            match price with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Prices.loadPageNext name id count

    let structure = {
            Headers = [
                { Label = "name" ; FlexBasis = 50; DataGetter = fun (item: Price) -> item.Name }
                { Label = "value" ; FlexBasis = 20; DataGetter = fun (item: Price) -> item.Value.ToString() }
                { Label = "provider_id" ; FlexBasis = 30; DataGetter = fun (item: Price) -> 
                    match Map.tryFind item.Provider_ID memoizedProviders with
                    | Some name -> name
                    | None -> "Unknown Provider" }
                { Label = "priceType" ; FlexBasis = 30; DataGetter = fun (item: Price) -> Constants.PriceTypeToText[item.PriceType] }
                { Label = "kind" ; FlexBasis = 30; DataGetter = fun (item: Price) -> Constants.EnergyKindToText[item.EnergyKind] }
            ]
            IdGetter = fun (item: Price) -> item.ID
        }

    let props = {|
            Structure = structure
            useEditor = fun price -> usePriceEditor price
            ItemNew = Utils.newPrice
            ItemSave = Api.Prices.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
