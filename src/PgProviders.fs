module PgProviders

open Feliz
open Lib
open WgEdit
open WgList

let useProviderEditor (provider: Provider) =
    let (name, setName) = React.useState(provider.Name)

    let fields = [
        StrField { Name = "name" ; Value = name; HandleChange = setName }
    ]

    let getUpdatedProvider () = 
        { provider with Name = name }

    fields, getUpdatedProvider


[<ReactComponent>]
let PgProviders() =

    let fetchBefore (provider: Provider option) count =
        let name, id =
            match provider with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Providers.loadPagePrev name id count


    let fetchAfter (provider: Provider option) count =
        let name, id =
            match provider with
                | Some x -> x.Name, x.ID
                | None -> "", ""
        Api.Providers.loadPageNext name id count

    let structure = {
            Headers = [
                { Label = "name" ; FlexBasis = 35; DataGetter = fun (item: Provider) -> item.Name }
            ]
            IdGetter = fun (item: Provider) -> item.ID
        }

    let props = {|
            Structure = structure
            useEditor = fun provider -> useProviderEditor provider
            ItemNew = fun () -> Utils.newProvider()
            ItemSave = Api.Providers.saveItem
            FetchBefore = fetchBefore
            FetchAfter = fetchAfter
        |}

    WgAgenda.WgAgenda props
