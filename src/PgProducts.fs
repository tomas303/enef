module PgProducts

open Feliz
open Lib
open WgEdit
open WgList

[<ReactComponent>]
let EditProduct (product: Product) onSave onCancel =

    let (provider_ID, setProvider_ID) = React.useState(product.Provider_ID)
    let (name, setName) = React.useState(product.Name)
    let (providers, setProviders) = React.useState([])

    React.useEffect((fun () -> (
        async {
            let! items = Lib.Api.Providers.loadAll()
            match items with
            | Ok content ->
                let newProviders = content |> List.map (fun x -> x.ID, x.Name)
                setProviders newProviders
            | Error _ ->
                setProviders []
        } |> Async.StartImmediate
        )), [| |])

    let edits = [
        StrField { Name = "name" ; Value = name; HandleChange = setName }
        SelectField { Name = "provider_ID" ; Value = provider_ID; Offer = providers; HandleChange = setProvider_ID }
    ]

    let handleSave () = 
        onSave { 
            product with
                Provider_ID = provider_ID
                Name = name}

    WgEdit edits handleSave onCancel


[<ReactComponent>]
let PgProducts() =


    let (providers, setProviders) = React.useState(Map.empty)

    React.useEffect((fun () -> 
        async {
            let! items = Api.Providers.loadAll()
            match items with
            | Ok content -> 
                Dbg.wl $"providers: {content}"
                let newProviders = content |> List.map (fun x -> x.ID, x.Name) |> Map.ofList
                setProviders newProviders
            | Error _ -> 
                setProviders Map.empty
        } |> Async.StartImmediate
    ), [||])

    let memoizedProviders = React.useMemo((fun () -> providers), [| providers |])

    let fetchBefore (product: Product option) count =
        let name, id =
            match product with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Products.loadPagePrev name id count


    let fetchAfter (product: Product option) count =
        let name, id =
            match product with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Products.loadPageNext name id count

    let structure: WgListStructure<Product> = {
            Headers = [
                { Label = "id" ; FlexBasis = 35; DataGetter = fun (item: Product) -> item.ID }
                { Label = "name" ; FlexBasis = 35; DataGetter = fun (item: Product) -> item.Name }
                { Label = "provider_ID" ; FlexBasis = 30; DataGetter = fun (item: Product) -> 
                    match Map.tryFind item.Provider_ID memoizedProviders with
                    | Some name -> name
                    | None -> "Unknown Provider" }

            ]
            IdGetter = fun (item: Product) -> item.ID
        }

    let props = {|
            Structure = structure
            NewEdit = fun product -> EditProduct product
            ItemNew = fun () -> Utils.newProduct()
            ItemSave = Api.Products.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
