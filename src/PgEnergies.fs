module PgEnergies

open Feliz
open Lib
open WgEdit
open WgList

let useEnergyEditor (energy: Energy) =
    let (kind, setKind) = React.useState(energy.Kind)
    let (created, setCreated) = React.useState(Utils.unixTimeToLocalDateTime(energy.Created))
    let (amount, setAmount) = React.useState(energy.Amount)
    let (place_id, setPlace_id) = React.useState(energy.Place_ID)
    let (places, setPlaces) = React.useState([])

    React.useEffect((fun () ->
        let energyCreated = Utils.unixTimeToLocalDateTime(energy.Created)
        if kind <> energy.Kind then 
            Browser.Dom.console.log("Setting kind")
            setKind(energy.Kind)
        if created <> energyCreated then 
            Browser.Dom.console.log("Setting created")
            setCreated(energyCreated)
        if amount <> energy.Amount then 
            Browser.Dom.console.log("Setting amount")
            setAmount(energy.Amount)
        if place_id <> energy.Place_ID then 
            Browser.Dom.console.log("Setting place_id")
            setPlace_id(energy.Place_ID)
    ))

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

    let fields = [
        DateTimeField { Name = "created"; Value = created; HandleChange = setCreated }
        SelectField { Name = "kind"; Value = Constants.EnergyKindToText.[kind]; Offer = Seq.toList Constants.TextToEnergyKind.Keys |> List.map (fun key -> key, key); HandleChange = (fun x -> setKind Constants.TextToEnergyKind.[x]) }
        IntField { Name = "amount"; Value = amount; HandleChange = setAmount }
        SelectField { Name = "place_id"; Value = place_id; Offer = places; HandleChange = setPlace_id }
    ]

    let getUpdatedEnergy () = 
        { energy with
            Amount = amount
            Created = Utils.localDateTimeToUnixTime(created)
            Kind = kind
            Place_ID = place_id }

    fields, getUpdatedEnergy


[<ReactComponent>]
let EditEnergy (energy: Energy) onSave onCancel =

    let (fields, getUpdatedEnergy) = useEnergyEditor energy

    let handleSave () = 
        onSave (getUpdatedEnergy ())

    WgEdit fields handleSave onCancel


[<ReactComponent>]
let PgEnergies() =

    let (places, setPlaces) = React.useState(Map.empty)

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

    let memoizedPlaces = React.useMemo((fun () -> places), [| places |])


    let fetchBefore energy count =
        let created, id =
            match energy with
                | Some x -> x.Created, x.ID
                | None -> 0, ""
        Api.Energies.loadPagePrev created id count


    let fetchAfter energy count =
        let created, id =
            match energy with
                | Some x -> x.Created, x.ID
                | None -> 0, ""
        Api.Energies.loadPageNext created id count

    let structure: WgListStructure<Energy> = {
            Headers = [
                { Label = "kind" ; FlexBasis = 15; DataGetter = fun item -> Constants.EnergyKindToText.[item.Kind] }
                { Label = "created" ; FlexBasis = 25; DataGetter = fun item -> (Utils.unixTimeToLocalDateTime item.Created).ToString("dd.MM.yyyy") }
                { Label = "amount" ; FlexBasis = 25; DataGetter = fun item -> $"{item.Amount} {Constants.EnergyKindToUnit.[item.Kind]}" }
                { Label = "place_id" ; FlexBasis = 10; DataGetter = fun item -> memoizedPlaces[item.Place_ID] }
            ]
            IdGetter = fun item -> item.ID
        }

    let props = {|
            Structure = structure
            useEditor = useEnergyEditor
            ItemNew = fun () -> Utils.newEnergy()
            ItemSave = Api.Energies.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
