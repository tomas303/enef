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
    let (priceType, setPriceType) = React.useState(price.PriceType)
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
        SelectField { Name = "priceType" ; Value = Constants.PriceTypeToText[priceType]; Offer = Constants.PriceTypeSelection; HandleChange = (fun x -> setPriceType Constants.TextToPriceType[x]) }
    ]

    let handleSave () = 
        onSave { 
            price with
                Value = value
                FromDate = fromDate
                Provider_ID = provider_ID
                PriceType = priceType }

    WgEdit edits handleSave onCancel


[<ReactComponent>]
let PgPrices() =

    let (providers, setProviders) = React.useState(Map.empty)

    React.useEffect((fun () -> 
        async {
            let! items = Api.Providers.loadAll()
            match items with
            | Ok content -> 
                Dbg.wl $"providers: {content}"
                let newProviders = content |> List.map (fun x -> x.ID, x.Name) |> Map.ofList
                setProviders newProviders
            | Error _ -> 
                setProviders Map.empty
        } |> Async.StartImmediate
    ), [||])

    let memoizedProviders = React.useMemo((fun () -> providers), [| providers |])

    let fetchBefore (price: Price option) count =
        let fromDate, id =
            match price with
                | Some x -> x.FromDate, x.ID
                | None -> "", ""
        Api.Prices.loadPagePrev fromDate id count


    let fetchAfter (price: Price option) count =
        let fromDate, id =
            match price with
                | Some x -> x.FromDate, x.ID
                | None -> "", ""
        Api.Prices.loadPageNext fromDate id count

    let structure = {
            Headers = [
                { Label = "value" ; FlexBasis = 20; DataGetter = fun (item: Price) -> item.Value.ToString() }
                { Label = "fromDate" ; FlexBasis = 20; DataGetter = fun (item: Price) -> item.FromDate }
                { Label = "provider_ID" ; FlexBasis = 30; DataGetter = fun (item: Price) -> 
                    match Map.tryFind item.Provider_ID memoizedProviders with
                    | Some name -> name
                    | None -> "Unknown Provider" }
                { Label = "priceType" ; FlexBasis = 30; DataGetter = fun (item: Price) -> Constants.PriceTypeToText[item.PriceType] }
            ]
            IdGetter = fun (item: Price) -> item.ID
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
