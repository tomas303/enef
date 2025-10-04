module PgEnergyPrices

open Feliz
open Lib
open WgEdit
open WgList

let useEnergyPriceEditor (ep: EnergyPrice) =
    let fromdate, setFromdate = React.useState(Utils.unixTimeToLocalDateTime(ep.FromDate))
    let price_id, setPrice_id = React.useState(ep.Price_ID)
    let prices, setPrices = React.useState([])
    let place_id, setPlace_id = React.useState(ep.Place_ID)
    let places, setPlaces = React.useState([])

    React.useEffect((fun () ->
        let energyPriceFromDate = Utils.unixTimeToLocalDateTime(ep.FromDate)
        setFromdate(energyPriceFromDate)
        setPrice_id(ep.Price_ID)
        setPlace_id(ep.Place_ID)
    ), [| box ep |])

    React.useEffect((fun () -> (
        async {
            let! items = Lib.Api.Places.loadAll()
            match items with
            | Ok content ->
                let newPlaces = content |> List.map (fun x -> x.ID, x.Name)
                setPlaces newPlaces
            | Error _ ->
                setPlaces []
        } |> Async.StartImmediate
        )), [| |])

    React.useEffect((fun () -> (
        async {
            let! items = Lib.Api.Prices.loadAll()
            match items with
            | Ok content ->
                let newPrices = content |> List.map (fun x -> x.ID, x.Name)
                setPrices newPrices
            | Error _ ->
                setPrices []
        } |> Async.StartImmediate
        )), [| |])

    let fields = [
        DateTimeField { Name = "fromdate"; Value = fromdate; HandleChange = setFromdate }
        SelectField { Name = "price_id"; Value = price_id; Offer = prices; HandleChange = setPrice_id }
        SelectField { Name = "place_id"; Value = place_id; Offer = places; HandleChange = setPlace_id }
    ]

    let getUpdatedEnergyPrice () = 
        { ep with
            FromDate = Utils.localDateTimeToUnixTime(fromdate)
            Price_ID = price_id
            Place_ID = place_id }

    fields, getUpdatedEnergyPrice


[<ReactComponent>]
let PgEnergyPrices() =

    let (places, setPlaces) = React.useState(Map.empty)
    let (prices, setPrices) = React.useState(Map.empty)

    React.useEffectOnce(fun () ->
        let fetchPlaces = async {
            let! items = Lib.Api.Places.loadAll()
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
            let! items = Lib.Api.Prices.loadAll()
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
    let memoizedPrices = React.useMemo((fun () -> prices), [| prices |])

    let fetchBefore energyPrice count =
        let fromDate, id =
            match energyPrice with
                | Some x -> x.FromDate, x.ID
                | None -> 0, ""
        Api.EnergyPrices.loadPagePrev fromDate id count

    let fetchAfter energyPrice count =
        let fromDate, id =
            match energyPrice with
                | Some x -> x.FromDate, x.ID
                | None -> 0, ""
        Api.EnergyPrices.loadPageNext fromDate id count

    let structure: WgListStructure<EnergyPrice> = {
            Headers = [
                { Label = "fromdate" ; FlexBasis = 25; DataGetter = fun item -> (Utils.unixTimeToLocalDateTime item.FromDate).ToString("dd.MM.yyyy") }
                { Label = "price_id" ; FlexBasis = 30; DataGetter = fun item ->
                    match Map.tryFind item.Price_ID memoizedPrices with
                    | Some name -> name
                    | None -> "Unknown price" }
                { Label = "place_id" ; FlexBasis = 30; DataGetter = fun item ->
                    match Map.tryFind item.Place_ID memoizedPlaces with
                    | Some name -> name
                    | None -> "Unknown place" }
            ]
            IdGetter = fun item -> item.ID
        }

    let props = {|
            Structure = structure
            useEditor = useEnergyPriceEditor
            ItemNew = Utils.newEnergyPrice
            ItemSave = Api.EnergyPrices.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
