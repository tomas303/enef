module PgProductPrices

open Feliz
open Lib
open WgEdit
open WgList
open Hooks

let useProductPriceEditor (pp: ProductPrice) =
    let fromdate, setFromdate = React.useState(Utils.unixTimeToLocalDateTime pp.FromDate)
    let product_id, setProduct_id = React.useState pp.Product_ID
    let products = useProducts().Data
    let value, setValue = React.useState pp.Value

    React.useEffect((fun () ->
        let energyPriceFromDate = Utils.unixTimeToLocalDateTime(pp.FromDate)
        setFromdate energyPriceFromDate
        setProduct_id pp.Product_ID
        setValue pp.Value
    ), [| box pp |])

    let fields = [
        DateTimeField { Name = "fromdate"; Value = fromdate; HandleChange = setFromdate }
        SelectField { Name = "place_id"; Value = product_id; Offer = products; HandleChange = setProduct_id }
        IntField { Name = "value" ; Value = value; HandleChange = setValue }
    ]

    let getUpdatedProductPrice () = 
        { pp with
            FromDate = Utils.localDateTimeToUnixTime(fromdate)
            Product_ID = product_id
            Value = value }

    fields, getUpdatedProductPrice


[<ReactComponent>]
let PgProductPrices() =

    let productsMap = useProducts().Data |> Map.ofList

    let fetchBefore (productPrice: ProductPrice option) count =
        let fromDate, id =
            match productPrice with
                | Some x -> x.FromDate, x.ID
                | None -> 0, ""
        Api.ProductPrices.loadPagePrev fromDate id count

    let fetchAfter (productPrice: ProductPrice option) count =
        let fromDate, id =
            match productPrice with
                | Some x -> x.FromDate, x.ID
                | None -> 0, ""
        Api.ProductPrices.loadPageNext fromDate id count

    let structure: WgListStructure<ProductPrice> = {
            Headers = [
                { Label = "fromdate" ; FlexBasis = 25; DataGetter = fun item -> (Utils.unixTimeToLocalDateTime item.FromDate).ToString("dd.MM.yyyy") }
                { Label = "product_id" ; FlexBasis = 30; DataGetter = fun item ->
                    match Map.tryFind item.Product_ID productsMap with
                    | Some name -> name
                    | None -> "Unknown product" }
                { Label = "value" ; FlexBasis = 20; DataGetter = fun (item: ProductPrice) -> item.Value.ToString() }
            ]
            IdGetter = fun item -> item.ID
        }

    let props = {|
            Structure = structure
            useEditor = useProductPriceEditor
            ItemNew = Utils.newProductPrice
            ItemSave = Api.ProductPrices.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
