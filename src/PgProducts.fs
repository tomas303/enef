module PgProducts

open Feliz
open Lib
open WgEdit
open WgList
open Hooks

let useProductEditor (product: Product) =
    let energyKind, setEnergyKind = React.useState product.EnergyKind
    let priceType, setPriceType = React.useState product.PriceType
    let provider_id, setProvider_id = React.useState product.Provider_ID
    let providers = useProviders().Data
    let name, setName = React.useState product.Name

    React.useEffect((fun () ->
        setEnergyKind product.EnergyKind
        setPriceType product.PriceType
        setName product.Name
        setProvider_id product.Provider_ID
    ), [| box product |])

    let fields = [
        SelectField { Name = "energyKind" ; Value = Constants.EnergyKindToText[energyKind]; Offer = Constants.EnergyKindSelection; HandleChange = fun x -> setEnergyKind Constants.TextToEnergyKind[x] }
        SelectField { Name = "priceType" ; Value = Constants.PriceTypeToText[priceType]; Offer = Constants.PriceTypeSelection; HandleChange = fun x -> setPriceType Constants.TextToPriceType[x] }
        SelectField { Name = "provider_id" ; Value = provider_id; Offer = providers; HandleChange = setProvider_id }
        StrField { Name = "name" ; Value = name; HandleChange = setName }
    ]

    let getUpdatedProduct () = 
        { product with
            EnergyKind = energyKind 
            PriceType = priceType
            Provider_ID = provider_id
            Name = name }

    fields, getUpdatedProduct


[<ReactComponent>]
let PgProducts() =

    let providersMap = useProviders().Data |> Map.ofList
    let productsCtx = useProducts()

    let saveAndRefresh item = async {
        let! result = Api.Products.saveItem item
        productsCtx.Refresh()
        return result
    }

    let fetchBefore (price: Product option) count =
        let name, id =
            match price with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Products.loadPagePrev name id count


    let fetchAfter (price: Product option) count =
        let name, id =
            match price with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Products.loadPageNext name id count

    let structure = {
            Headers = [
                { Label = "name" ; FlexBasis = 50; DataGetter = fun (item: Product) -> item.Name }
                { Label = "provider_id" ; FlexBasis = 30; DataGetter = fun (item: Product) -> 
                    match Map.tryFind item.Provider_ID providersMap with
                    | Some name -> name
                    | None -> "Unknown Provider" }
                { Label = "priceType" ; FlexBasis = 30; DataGetter = fun (item: Product) -> Constants.PriceTypeToText[item.PriceType] }
                { Label = "kind" ; FlexBasis = 30; DataGetter = fun (item: Product) -> Constants.EnergyKindToText[item.EnergyKind] }
            ]
            IdGetter = fun (item: Product) -> item.ID
        }

    let props = {|
            Structure = structure
            useEditor = fun price -> useProductEditor price
            ItemNew = Utils.newProduct
            ItemSave = saveAndRefresh
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
