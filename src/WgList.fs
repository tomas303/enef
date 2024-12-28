module WgList

open Feliz
open Feliz.UseListener

type WgListHeader = {
    Label: string
    FlexBasis: int
}

[<ReactComponent>]
let WgListCell (key:string) (value: string) (flexBasis: int) =
    Html.div [ 
        prop.key key
        prop.classes [ "fg-scell" ]
        prop.style [ style.custom ("--flex-basis", $"{flexBasis}%%") ]
        prop.children [ Html.text value ] 
    ]

[<ReactComponent>]
let WgListCellInvisible (key:string) (flexBasis: int) =
    let invisible = Html.div [ prop.style [ style.visibility.hidden ]; prop.children [Html.text " x "]  ]
    Html.div [
        prop.key key
        prop.classes [ "fg-scell" ]
        prop.style [ style.custom ("--flex-basis", $"{flexBasis}%%") ]
        prop.children [ invisible ] 
    ]

[<ReactComponent>]
let WgListRow (key:string) (cells: ReactElement list) =
    Html.div [
        prop.key key
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
let WgList (props:{|
        Headers: WgListHeader list
        Rows: (string * string list) list
        LoadingInProgress: bool
        RowCount: int
        Cursor: int
        OnPageUp: unit -> unit
        OnPageDown: unit -> unit
        OnRowUp: unit -> unit
        OnRowDown: unit -> unit
        OnAdd: unit -> unit
    |}) =


    let cursor active = 
        if active
        then WgListCell "cursor" ">" 5
        else WgListCell "cursor" " " 5

    React.useListener.onKeyDown(fun ev ->
        match props.LoadingInProgress with
        | false ->
            match ev.key with
            | "PageUp" -> props.OnPageUp(); ev.preventDefault()
            | "PageDown" -> props.OnPageDown(); ev.preventDefault()
            | "ArrowUp" -> props.OnRowUp(); ev.preventDefault()
            | "ArrowDown" -> props.OnRowDown(); ev.preventDefault()
            | _ -> ()
        | _ -> ()
    )

    let prev = Html.button [
        prop.text "Prev"
        prop.onClick (fun _ -> props.OnPageUp())
        prop.disabled props.LoadingInProgress
    ]
    let next = Html.button [
        prop.text "Next"
        prop.onClick (fun _ -> props.OnPageDown())
        prop.disabled props.LoadingInProgress
    ]
    let add = Html.button [
        prop.text "Add"
        prop.onClick (fun _ -> props.OnAdd())
    ]

    let rows = props.Rows |> List.mapi (fun idx (key, row) -> 
        let cells = List.map2 (fun h r -> WgListCell h.Label r h.FlexBasis) props.Headers row
        WgListRow key ([ cursor(props.Cursor = idx) ] @ cells)
    )

    let invrows = 
        [ for i in 1..props.RowCount - rows.Length -> 
            let cells = props.Headers |> List.map (fun h -> WgListCellInvisible h.Label h.FlexBasis)
            WgListRow ("invisible_" + i.ToString()) ([ cursor(false) ] @ cells)
        ]

    let allRows = rows @ invrows

    let buttons = Html.div [ prev; next; add ]
    
    Html.div [
        WgListGrid allRows
        buttons
    ]

