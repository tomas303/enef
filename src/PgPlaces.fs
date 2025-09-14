module PgPlaces

open Feliz
open Lib
open WgEdit
open WgList

let usePlaceEditor (place: Place) =
    let (circuitBreakerCurrent, setCircuitBreakerCurrent) = React.useState(place.CircuitBreakerCurrent)
    let (name, setName) = React.useState(place.Name)

    let fields = [
        StrField { Name = "name" ; Value = name; HandleChange = setName }
        IntField { Name = "circuitBreakerCurrent" ; Value = circuitBreakerCurrent; HandleChange = setCircuitBreakerCurrent }
    ]

    let getUpdatedPlace () = 
        { place with
            CircuitBreakerCurrent = circuitBreakerCurrent
            Name = name }

    fields, getUpdatedPlace


[<ReactComponent>]
let PgPlaces() =

    let fetchBefore (place: Place option) count =
        let name, id =
            match place with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Places.loadPagePrev name id count


    let fetchAfter (place: Place option) count =
        let name, id =
            match place with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Places.loadPageNext name id count

    let structure = {
            Headers = [
                { Label = "name" ; FlexBasis = 35; DataGetter = fun (item: Place) -> item.Name }
                { Label = "circuitBreakerCurrent" ; FlexBasis = 15; DataGetter = fun (item: Place) -> $"{item.CircuitBreakerCurrent} A" }
            ]
            IdGetter = fun (item: Place) -> item.ID
        }

    let props = {|
            Structure = structure
            useEditor = fun place -> usePlaceEditor place
            ItemNew = fun () -> Utils.newPlace()
            ItemSave = Api.Places.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
