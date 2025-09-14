module PgPlaceProducts

open Feliz
open Lib
open WgEdit
open WgList

let usePlaceProductEditor (pp: PlaceProduct) =
    let (fromDate, setFromDate) = React.useState(pp.FromDate)
    let (place_ID, setPlace_ID) = React.useState(pp.Place_ID)
    let (product_ID, setProduct_ID) = React.useState(pp.Product_ID)
    let (places, setPlaces) = React.useState([])
    let (products, setProducts) = React.useState([])

    React.useEffect((fun () -> (
        async {
            let! items = Lib.Api.Places.loadAll()
            match items with
            | Ok content ->
                let newPlaces = content |> List.map (fun x -> x.ID, x.Name)
                setPlaces newPlaces
                if place_ID = "" && newPlaces.Length > 0 then 
                    let id, name = newPlaces[0]
                    setPlace_ID id
            | Error _ ->
                setPlaces []
        } |> Async.StartImmediate
        )), [| |])

    React.useEffect((fun () -> (
        async {
            let! items = Lib.Api.Products.loadAll()
            match items with
            | Ok content ->
                let newProducts = content |> List.map (fun x -> x.ID, x.Name)
                setProducts newProducts
                if product_ID = "" && newProducts.Length > 0 then 
                    let id, name = newProducts[0]
                    setProduct_ID id
            | Error _ ->
                setProducts []
        } |> Async.StartImmediate
        )), [| |])

    let fields = [
        StrField { Name = "fromDate" ; Value = fromDate; HandleChange = setFromDate }
        SelectField { Name = "place_ID" ; Value = place_ID; Offer = places; HandleChange = setPlace_ID }
        SelectField { Name = "product_ID" ; Value = product_ID; Offer = products; HandleChange = setProduct_ID }
    ]

    let getUpdatedPlaceProduct () = 
        { pp with
            FromDate = fromDate
            Place_ID = place_ID
            Product_ID = product_ID }

    fields, getUpdatedPlaceProduct


[<ReactComponent>]
let PgPlaceProducts() =

    let (places, setPlaces) = React.useState(Map.empty)
    let (products, setProducts) = React.useState(Map.empty)

    React.useEffect((fun () -> 
        async {
            let! items = Api.Places.loadAll()
            match items with
            | Ok content -> 
                let newPlaces = content |> List.map (fun x -> x.ID, x.Name) |> Map.ofList
                setPlaces newPlaces
            | Error _ -> 
                setPlaces Map.empty
        } |> Async.StartImmediate
    ), [||])

    let memoizedPlaces = React.useMemo((fun () -> places), [| places |])

    React.useEffect((fun () -> 
        async {
            let! items = Api.Products.loadAll()
            match items with
            | Ok content -> 
                let newProducts = content |> List.map (fun x -> x.ID, x.Name) |> Map.ofList
                setProducts newProducts
            | Error _ -> 
                setProducts Map.empty
        } |> Async.StartImmediate
    ), [||])

    let memoizedProducts = React.useMemo((fun () -> products), [| products |])

    let fetchBefore (pp: PlaceProduct option) count =
        let fromDate, id =
            match pp with
                | Some x -> x.FromDate, x.ID
                | None -> "", ""
        Api.PlaceProducts.loadPagePrev fromDate id count


    let fetchAfter (pp: PlaceProduct option) count =
        let fromDate, id =
            match pp with
                | Some x -> x.FromDate, x.ID
                | None -> "", ""
        Api.PlaceProducts.loadPageNext fromDate id count

    let structure = {
            Headers = [
                { Label = "fromDate" ; FlexBasis = 20; DataGetter = fun (item: PlaceProduct) -> item.FromDate }
                { Label = "place_ID" ; FlexBasis = 30; DataGetter = fun (item: PlaceProduct) -> 
                    match Map.tryFind item.Place_ID memoizedPlaces with
                    | Some name -> name
                    | None -> "Unknown Place" }
                { Label = "product_ID" ; FlexBasis = 30; DataGetter = fun (item: PlaceProduct) -> 
                    match Map.tryFind item.Product_ID memoizedProducts with
                    | Some name -> name
                    | None -> "Unknown Product" }
            ]
            IdGetter = fun (item: PlaceProduct) -> item.ID
        }

    let props = {|
            Structure = structure
            useEditor = fun pp -> usePlaceProductEditor pp
            ItemNew = fun () -> Utils.newPlaceProduct()
            ItemSave = Api.PlaceProducts.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
