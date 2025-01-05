module PgPlaces

open Feliz
open Lib
open WgEdit
open WgList

[<ReactComponent>]
let EditPlace place onSave onCancel =

    let (circuitBreakerCurrent, setCircuitBreakerCurrent) = React.useState(place.CircuitBreakerCurrent)
    let (name, setName) = React.useState(place.Name)

    let edits = [
        StrField { Name = "name" ; Value = name; HandleChange = setName }
        IntField { Name = "circuitBreakerCurrent" ; Value = circuitBreakerCurrent; HandleChange = setCircuitBreakerCurrent }
    ]

    let handleSave () = 
        onSave { 
            place with
                CircuitBreakerCurrent = circuitBreakerCurrent
                Name = name}

    WgEdit edits handleSave onCancel


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
            NewEdit = fun place -> EditPlace place
            ItemNew = fun () -> Utils.newPlace()
            ItemSave = Api.Places.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
