[<RequireQualifiedAccess>]
module App

open Feliz
open PgEnergies
open PgPlaces
open PgPlaceProducts
open PgProviders
open PgProducts
open PgProductPrices
open PgPriceSeries
open PgTest

[<RequireQualifiedAccess>]
type Page =
    | Home
    | Pricelists
    | Energies
    | Products
    | ProductPrices
    | Places
    | PlaceProducts
    | Providers
    | PriceSeries
    | Test
    
let renderMenuNavItem (label : string) (handler : unit -> unit) =
    Html.a [
        prop.onClick (fun _ -> handler ())
        prop.children [ Html.text label ]
    ]

[<ReactComponent>]
let Menunav (menus : ReactElement list) =
    Html.nav [
        prop.classes [ "menu" ]
        prop.children menus
    ]

[<ReactComponent>]
let App () =

    let (currentPage, setCurrentPage) = React.useState Page.Home

    let menus = [
        renderMenuNavItem "Home" (fun _ -> setCurrentPage Page.Home)
        renderMenuNavItem "Energies" (fun _ -> setCurrentPage Page.Energies)
        renderMenuNavItem "Products" (fun _ -> setCurrentPage Page.Products)
        renderMenuNavItem "Product prices" (fun _ -> setCurrentPage Page.ProductPrices)
        renderMenuNavItem "Places" (fun _ -> setCurrentPage Page.Places)
        renderMenuNavItem "PlaceProducts" (fun _ -> setCurrentPage Page.PlaceProducts)
        renderMenuNavItem "Providers" (fun _ -> setCurrentPage Page.Providers)
        renderMenuNavItem "PriceSeries" (fun _ -> setCurrentPage Page.PriceSeries)
        renderMenuNavItem "Test" (fun _ -> setCurrentPage Page.Test)]

    let page =
        match currentPage with
        | Page.Home -> PgEnergies ()
        | Page.Pricelists -> Html.div "Pricelists - to be done"
        | Page.Energies -> PgEnergies ()
        | Page.Products -> PgProducts ()
        | Page.ProductPrices -> PgProductPrices ()
        | Page.Places -> PgPlaces ()
        | Page.PlaceProducts -> PgPlaceProducts ()
        | Page.Providers -> PgProviders ()
        | Page.PriceSeries -> PgPriceSeries ()
        | Page.Test -> PgTest ()

    Html.div [
        prop.classes [ "layout-container" ]
        prop.children [
            Html.div [
                prop.classes [ "layout-nav" ]
                prop.children [ Menunav menus ]
            ]
            Html.div [
                prop.classes [ "layout-content" ]
                prop.children [ page ]
            ]
        ]
    ]
