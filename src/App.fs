[<RequireQualifiedAccess>]
module App

open Feliz
open PgEnergies

[<RequireQualifiedAccess>]
type Page =
    | Home
    | Pricelists
    | Energies


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
    ]

    let page =
        match currentPage with
        | Page.Home -> PgEnergies ()
        | Page.Pricelists -> Html.div "Pricelists - to be done"
        | Page.Energies -> PgEnergies ()

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
