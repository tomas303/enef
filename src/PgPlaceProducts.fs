module PgPlaceProducts

open Feliz
open Lib
open WgEdit
open WgList
open Hooks

let usePlaceProductEditor (ep: PlaceProduct) =
    let fromdate, setFromdate = React.useState(Utils.unixTimeToLocalDateTime ep.FromDate)
    let product_id, setPrice_id = React.useState ep.Product_ID
    let products = useProducts().Data
    let place_id, setPlace_id = React.useState ep.Place_ID
    let places = usePlaces().Data

    React.useEffect((fun () ->
        let energyPriceFromDate = Utils.unixTimeToLocalDateTime ep.FromDate
        setFromdate energyPriceFromDate
        setPrice_id ep.Product_ID
        setPlace_id ep.Place_ID
    ), [| box ep |])

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

    let placesMap = usePlaces().Data |> Map.ofList
    let productsMap = useProducts().Data |> Map.ofList

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
                    match Map.tryFind item.Product_ID productsMap with
                    | Some name -> name
                    | None -> "Unknown product" }
                { Label = "place_id" ; FlexBasis = 30; DataGetter = fun item ->
                    match Map.tryFind item.Place_ID placesMap with
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
