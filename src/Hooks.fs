module Hooks

open Feliz
open Lib

let ProvidersContext = React.createContext<list<string * string>>("ProvidersContext", [])
let PlacesContext    = React.createContext<list<string * string>>("PlacesContext", [])
let ProductsContext  = React.createContext<list<string * string>>("ProductsContext", [])

let useProviders () = React.useContext ProvidersContext
let usePlaces ()    = React.useContext PlacesContext
let useProducts ()  = React.useContext ProductsContext

[<ReactComponent>]
let AppDataProvider (children: ReactElement list) =
    let providers, setProviders = React.useState []
    let places, setPlaces       = React.useState []
    let products, setProducts   = React.useState []

    React.useEffectOnce(fun () ->
        async {
            let! result = Api.Providers.loadAll()
            match result with
            | Ok content -> setProviders (content |> List.map (fun x -> x.ID, x.Name))
            | Error _ -> ()
        } |> Async.StartImmediate
    )

    React.useEffectOnce(fun () ->
        async {
            let! result = Api.Places.loadAll()
            match result with
            | Ok content -> setPlaces (content |> List.map (fun x -> x.ID, x.Name))
            | Error _ -> ()
        } |> Async.StartImmediate
    )

    React.useEffectOnce(fun () ->
        async {
            let! result = Api.Products.loadAll()
            match result with
            | Ok content -> setProducts (content |> List.map (fun x -> x.ID, x.Name))
            | Error _ -> ()
        } |> Async.StartImmediate
    )

    React.contextProvider(ProvidersContext, providers, [
        React.contextProvider(PlacesContext, places, [
            React.contextProvider(ProductsContext, products, children)
        ])
    ])
