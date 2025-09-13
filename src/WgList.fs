module WgList

open Feliz
open Feliz.UseListener
open CustomElements

type WgListHeader<'T> = {
    Label: string
    FlexBasis: int
    DataGetter: 'T -> string
}

type WgListStructure<'T> = {
    Headers: List<WgListHeader<'T>>
    IdGetter: 'T -> string
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
        Structure: WgListStructure<'T>
        Rows: (string * string list) list
        RowCount: int
        Cursor: int
    |}) =


    let cursor active = 
        if active
        then WgListCell "cursor" ">" 5
        else WgListCell "cursor" " " 5

    let rows = props.Rows |> List.mapi (fun idx (key, row) -> 
        let cells = List.map2 (fun h r -> WgListCell h.Label r h.FlexBasis) props.Structure.Headers row
        WgListRow key ([ cursor(props.Cursor = idx) ] @ cells)
    )

    let invrows = 
        [ for i in 1..props.RowCount - rows.Length -> 
            let cells = props.Structure.Headers |> List.map (fun h -> WgListCellInvisible h.Label h.FlexBasis)
            WgListRow ("invisible_" + i.ToString()) ([ cursor(false) ] @ cells)
        ]

    let allRows = rows @ invrows
   
    Html.div [
        WgListGrid allRows
    ]

