module PgPlaceProducts

open Feliz
open Lib
open WgEdit
open WgList

let usePlaceProductEditor (ep: PlaceProduct) =
    let fromdate, setFromdate = React.useState(Utils.unixTimeToLocalDateTime ep.FromDate)
    let product_id, setPrice_id = React.useState ep.Product_ID
    let products, setProducts = React.useState []
    let place_id, setPlace_id = React.useState ep.Place_ID
    let places, setPlaces = React.useState []

    React.useEffect((fun () ->
        let energyPriceFromDate = Utils.unixTimeToLocalDateTime ep.FromDate
        setFromdate energyPriceFromDate
        setPrice_id ep.Product_ID
        setPlace_id ep.Place_ID
    ), [| box ep |])

    React.useEffect((fun () -> 
        async {
            let! items = Lib.Api.Places.loadAll()
            match items with
            | Ok content ->
                let newPlaces = content |> List.map (fun x -> x.ID, x.Name)
                setPlaces newPlaces
            | Error _ ->
                setPlaces []
        } |> Async.StartImmediate
        ), [| |])

    React.useEffect((fun () -> 
        async {
            let! items = Lib.Api.Products.loadAll()
            match items with
            | Ok content ->
                let newProducts = content |> List.map (fun x -> x.ID, x.Name)
                setProducts newProducts
            | Error _ ->
                setProducts []
        } |> Async.StartImmediate
        ), [| |])

    let fields = [
        DateTimeField { Name = "fromdate"; Value = fromdate; HandleChange = setFromdate }
        SelectField { Name = "product_id"; Value = product_id; Offer = products; HandleChange = setPrice_id }
        SelectField { Name = "place_id"; Value = place_id; Offer = places; HandleChange = setPlace_id }
    ]

    let getUpdatedPlaceProduct () = 
        { ep with
            FromDate = Utils.localDateTimeToUnixTime(fromdate)
            Product_ID = product_id
            Place_ID = place_id }

    fields, getUpdatedPlaceProduct


[<ReactComponent>]
let PgPlaceProducts() =

    let places, setPlaces = React.useState Map.empty
    let prices, setPrices = React.useState Map.empty

    React.useEffectOnce(fun () ->
        let fetchPlaces = async {
            let! items = Api.Places.loadAll()
            match items with
            | Ok content ->
                let newPlaces = content |> List.map (fun x -> x.ID, x.Name) |> Map.ofList
                setPlaces newPlaces
            | Error _ ->
                setPlaces Map.empty
        }
        Async.StartImmediate fetchPlaces
    )

    React.useEffectOnce(fun () ->
        let fetchPrices = async {
            let! items = Api.Products.loadAll()
            match items with
            | Ok content ->
                let newPrices = content |> List.map (fun x -> x.ID, x.Name) |> Map.ofList
                setPrices newPrices
            | Error _ ->
                setPrices Map.empty
        }
        Async.StartImmediate fetchPrices
    )

    let memoizedPlaces = React.useMemo((fun () -> places), [| places |])
    let memoizedProducts = React.useMemo((fun () -> prices), [| prices |])

    let fetchBefore placeProduct count =
        let fromDate, id =
            match placeProduct with
                | Some x -> x.FromDate, x.ID
                | None -> 0, ""
        Api.PlaceProducts.loadPagePrev fromDate id count

    let fetchAfter placeProduct count =
        let fromDate, id =
            match placeProduct with
                | Some x -> x.FromDate, x.ID
                | None -> 0, ""
        Api.PlaceProducts.loadPageNext fromDate id count

    let structure: WgListStructure<PlaceProduct> = {
            Headers = [
                { Label = "fromdate" ; FlexBasis = 25; DataGetter = fun item -> (Utils.unixTimeToLocalDateTime item.FromDate).ToString("dd.MM.yyyy") }
                { Label = "product_id" ; FlexBasis = 30; DataGetter = fun item ->
                    match Map.tryFind item.Product_ID memoizedProducts with
                    | Some name -> name
                    | None -> "Unknown product" }
                { Label = "place_id" ; FlexBasis = 30; DataGetter = fun item ->
                    match Map.tryFind item.Place_ID memoizedPlaces with
                    | Some name -> name
                    | None -> "Unknown place" }
            ]
            IdGetter = fun item -> item.ID
        }

    let props = {|
            Structure = structure
            useEditor = usePlaceProductEditor
            ItemNew = Utils.newPlaceProduct
            ItemSave = Api.PlaceProducts.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
