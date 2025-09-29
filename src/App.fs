[<RequireQualifiedAccess>]
module App

open Feliz
open PgEnergies
open PgPlaces
open PgProviders
open PgPrices
open PgEnergyPrices
open PgTest

[<RequireQualifiedAccess>]
type Page =
    | Home
    | Pricelists
    | Energies
    | EnergyPrices
    | Places
    | Providers
    | Prices
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
        renderMenuNavItem "Energy prices" (fun _ -> setCurrentPage Page.EnergyPrices)
        renderMenuNavItem "Prices" (fun _ -> setCurrentPage Page.Prices)
        renderMenuNavItem "Places" (fun _ -> setCurrentPage Page.Places)
        renderMenuNavItem "Providers" (fun _ -> setCurrentPage Page.Providers)
        renderMenuNavItem "Test" (fun _ -> setCurrentPage Page.Test)]

    let page =
        match currentPage with
        | Page.Home -> PgEnergies ()
        | Page.Pricelists -> Html.div "Pricelists - to be done"
        | Page.Energies -> PgEnergies ()
        | Page.EnergyPrices -> PgEnergyPrices ()
        | Page.Places -> PgPlaces ()
        | Page.Providers -> PgProviders ()
        | Page.Prices -> PgPrices ()
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
