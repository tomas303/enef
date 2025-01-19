module PgPrices

open Feliz
open Lib
open WgEdit
open WgList

[<ReactComponent>]
let EditPrice (price: Price) onSave onCancel =

    let (value, setValue) = React.useState(price.Value)
    let (fromDate, setFromDate) = React.useState(price.FromDate)
    let (product_ID, setProduct_ID) = React.useState(price.Product_ID)
    let (priceType, setPriceType) = React.useState(price.PriceType)
    let (energyKind, setEnergyKind) = React.useState(price.EnergyKind)
    let (products, setProducts) = React.useState([])

    React.useEffect((fun () -> (
        async {
            let! items = Lib.Api.Products.loadAll()
            match items with
            | Ok content ->
                let newProducts = content |> List.map (fun x -> x.ID, x.Name)
                setProducts newProducts
            | Error _ ->
                setProducts []
        } |> Async.StartImmediate
        )), [| |])


    let edits = [
        IntField { Name = "value" ; Value = value; HandleChange = setValue }
        StrField { Name = "fromDate" ; Value = fromDate; HandleChange = setFromDate }
        SelectField { Name = "energyKind" ; Value = Constants.EnergyKindToText[energyKind]; Offer = Constants.EnergyKindSelection; HandleChange = (fun x -> setEnergyKind Constants.TextToEnergyKind[x]) }
        SelectField { Name = "priceType" ; Value = Constants.PriceTypeToText[priceType]; Offer = Constants.PriceTypeSelection; HandleChange = (fun x -> setPriceType Constants.TextToPriceType[x]) }
        SelectField { Name = "product_ID" ; Value = product_ID; Offer = products; HandleChange = setProduct_ID }
    ]

    let handleSave () = 
        onSave { 
            price with
                Value = value
                FromDate = fromDate
                Product_ID = product_ID
                PriceType = priceType
                EnergyKind = energyKind }

    WgEdit edits handleSave onCancel


[<ReactComponent>]
let PgPrices() =

    let (products, setProducts) = React.useState(Map.empty)

    React.useEffect((fun () -> 
        async {
            let! items = Api.Products.loadAll()
            match items with
            | Ok content -> 
                Dbg.wl $"products: {content}"
                let newProducts = content |> List.map (fun x -> x.ID, x.Name) |> Map.ofList
                setProducts newProducts
            | Error _ -> 
                setProducts Map.empty
        } |> Async.StartImmediate
    ), [||])

    let memoizedProducts = React.useMemo((fun () -> products), [| products |])

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
                { Label = "product_ID" ; FlexBasis = 30; DataGetter = fun (item: Price) -> 
                    match Map.tryFind item.Product_ID memoizedProducts with
                    | Some name -> name
                    | None -> "Unknown Product" }
                { Label = "priceType" ; FlexBasis = 30; DataGetter = fun (item: Price) -> Constants.PriceTypeToText[item.PriceType] }
                { Label = "kind" ; FlexBasis = 30; DataGetter = fun (item: Price) -> Constants.EnergyKindToText[item.EnergyKind] }
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
