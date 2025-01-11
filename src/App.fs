[<RequireQualifiedAccess>]
module App

open Feliz
open PgEnergies
open PgPlaces
open PgProviders

[<RequireQualifiedAccess>]
type Page =
    | Home
    | Pricelists
    | Energies
    | Places
    | Providers

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
        renderMenuNavItem "Pricelists" (fun _ -> setCurrentPage Page.Pricelists)
        renderMenuNavItem "Places" (fun _ -> setCurrentPage Page.Places)
        renderMenuNavItem "Providers" (fun _ -> setCurrentPage Page.Providers)
    ]

    let page =
        match currentPage with
        | Page.Home -> PgEnergies ()
        | Page.Pricelists -> Html.div "Pricelists - to be done"
        | Page.Energies -> PgEnergies ()
        | Page.Places -> PgPlaces ()
        | Page.Providers -> PgProviders ()

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
