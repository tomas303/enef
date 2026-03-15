module Hooks

open Feliz
open Lib

type RefData = { Data: list<string * string>; Refresh: unit -> unit }

let private emptyRefData = { Data = []; Refresh = fun () -> () }

let ProvidersContext = React.createContext<RefData>("ProvidersContext", emptyRefData)
let PlacesContext    = React.createContext<RefData>("PlacesContext",    emptyRefData)
let ProductsContext  = React.createContext<RefData>("ProductsContext",  emptyRefData)

let useProviders () = React.useContext ProvidersContext
let usePlaces ()    = React.useContext PlacesContext
let useProducts ()  = React.useContext ProductsContext

[<ReactComponent>]
let AppDataProvider (children: ReactElement list) =
    let providers, setProviders = React.useState []
    let places, setPlaces       = React.useState []
    let products, setProducts   = React.useState []

    let fetchProviders () =
        async {
            let! result = Api.Providers.loadAll()
            match result with
            | Ok content -> setProviders (content |> List.map (fun x -> x.ID, x.Name))
            | Error _ -> ()
        } |> Async.StartImmediate

    let fetchPlaces () =
        async {
            let! result = Api.Places.loadAll()
            match result with
            | Ok content -> setPlaces (content |> List.map (fun x -> x.ID, x.Name))
            | Error _ -> ()
        } |> Async.StartImmediate

    let fetchProducts () =
        async {
            let! result = Api.Products.loadAll()
            match result with
            | Ok content -> setProducts (content |> List.map (fun x -> x.ID, x.Name))
            | Error _ -> ()
        } |> Async.StartImmediate

    React.useEffectOnce(fun () -> fetchProviders())
    React.useEffectOnce(fun () -> fetchPlaces())
    React.useEffectOnce(fun () -> fetchProducts())

    React.contextProvider(ProvidersContext, { Data = providers; Refresh = fetchProviders }, [
        React.contextProvider(PlacesContext, { Data = places; Refresh = fetchPlaces }, [
            React.contextProvider(ProductsContext, { Data = products; Refresh = fetchProducts }, children)
        ])
    ])
