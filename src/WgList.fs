module WgList

open Feliz
open Feliz.UseListener

type WgListHeader = {
    Label: string
    FlexBasis: int
}

[<ReactComponent>]
let WgListCell (value: string) (flexBasis: int) =
    Html.div [ 
        prop.classes [ "fg-scell" ]
        prop.style [ style.custom ("--flex-basis", $"{flexBasis}%%") ]
        prop.children [ Html.text value ] 
    ]

[<ReactComponent>]
let WgListCellInvisible (flexBasis: int) =
    let invisible = Html.div [ prop.style [ style.visibility.hidden ]; prop.children [Html.text " x "]  ]
    Html.div [ 
        prop.classes [ "fg-scell" ]
        prop.style [ style.custom ("--flex-basis", $"{flexBasis}%%") ]
        prop.children [ invisible ] 
    ]

[<ReactComponent>]
let WgListRow (cells: ReactElement list) =
    Html.div [
        prop.classes [ "fg-row" ]
        prop.children cells
    ]

[<ReactComponent>]
let WgListGrid (rows: ReactElement list) =
    Html.div [
        prop.classes [ "fg-grid" ]
        prop.children rows
    ]

[<ReactComponent>]
let WgList headers rows loadingInProgress rowCount onPrevPage onNextPage onPrevRow onNextRow onAdd =

    React.useListener.onKeyDown(fun ev ->
        match loadingInProgress with
        | false ->
            match ev.key with
            | "PageUp" -> onPrevPage(); ev.preventDefault()
            | "PageDown" -> onNextPage(); ev.preventDefault()
            | "ArrowUp" -> onPrevRow(); ev.preventDefault()
            | "ArrowDown" -> onNextRow(); ev.preventDefault()
            | _ -> ()
        | _ -> ()
        )

    let prev = Html.button [
        prop.text "Prev"
        prop.onClick (fun _ -> onPrevPage())
        prop.disabled loadingInProgress
    ]
    let next = Html.button [
        prop.text "Next"
        prop.onClick (fun _ -> onNextPage())
        prop.disabled loadingInProgress
    ]
    let add = Html.button [
        prop.text "Add"
        prop.onClick (fun _ -> onAdd())
    ]


    let rows = rows |> List.map(
        fun row -> 
            let cells =  headers |> List.map2 (fun r h -> WgListCell r h.FlexBasis) row
            WgListRow cells
        )

    let invrows = 
        [ for _ in 1..rowCount - rows.Length -> (
                let cells =  headers |> List.map (fun h -> WgListCellInvisible h.FlexBasis)
                WgListRow cells
            )
        ]
    let rows = rows @ invrows

    Html.div [
        Html.div (WgListGrid rows)
        Html.div [prev; next; add]
    ]
