module WgAgenda

open Feliz
open Feliz.UseDeferred
open Feliz.UseListener
open Browser.Dom
open WgList
open WgEdit
open CustomElements

module GridBuffer =

    type Data<'T> = {
        Top: int
        Bottom: int
        Cursor: int
        ViewSize: int
        DataSize: int
        Data: List<'T>
        Lasterror: Option<string>
    }   

    let applyDelta data delta = 
        let newCursor = data.Cursor + delta
        if newCursor < data.Top || newCursor > data.Bottom then
            let newTop = data.Top + delta
            {data with 
                Cursor = newCursor
                Top = newTop
                Bottom = newTop + data.ViewSize - 1
            }
        else { data with Cursor = newCursor }

    let readBefore data fetchBefore = async {
        let delta = abs(data.Top)
        let! fetchData =
            if data.Data.Length > 0 
            then fetchBefore (Some data.Data[0]) delta
            else fetchBefore None delta
        match fetchData with
        | Ok content ->
            let newData = content @ data.Data
            let newData = 
                if newData.Length > data.DataSize then
                    List.take data.DataSize newData
                else
                    newData
            return 
                { data with
                    Cursor = data.Cursor + content.Length
                    Top = data.Top + content.Length
                    Bottom = data.Bottom + content.Length
                    Data = newData }
        | Error error ->
            return { data with Lasterror = Some(error) }
        }

    let readAfter data fetchAfter = async {
        let delta = max data.ViewSize (abs(data.Bottom - data.Data.Length))
        let! fetchData = 
            if data.Data.Length > 0 
            then fetchAfter (Some data.Data[data.Data.Length - 1]) delta
            else fetchAfter None delta
        match fetchData with
        | Ok content ->
            let newData = data.Data @ content
            let removeCnt =
                if newData.Length > data.DataSize
                then newData.Length - data.DataSize
                else 0
            return 
                { data with
                    Cursor = data.Cursor - removeCnt
                    Top = data.Top - removeCnt
                    Bottom = data.Bottom - removeCnt
                    Data = List.skip removeCnt newData }
        | Error error ->
            return { data with Lasterror = Some(error) }
        }

    let correct data =
        let newTop =
            if data.Top < 0 then
                0
            else if data.Top > data.Data.Length - 1 then
                data.Data.Length - 1
            else
                data.Top
        let newBottom = 
            if newTop + data.ViewSize - 1 > data.Data.Length - 1
            then data.Data.Length - 1
            else newTop + data.ViewSize - 1
        let newCursor =
            if data.Cursor < newTop 
            then newTop
            else if data.Cursor > newBottom
            then newBottom
            else data.Cursor
        { data with
            Cursor = newCursor
            Top = newTop
            Bottom = newBottom }

    let move data delta fetchBefore fetchAfter = async {
        let newData = applyDelta data delta
        let! newData =
            if newData.Top < 0 then
                readBefore newData fetchBefore
            else if newData.Bottom > newData.Data.Length - 1 then
                readAfter newData fetchAfter
            else
                async { return newData }
        let newData = correct newData
        return newData
    }

    let view data =
        data.Data[data.Top..data.Bottom]

    let cursorValid data =
        data.Cursor >= 0 && data.Cursor <= data.Data.Length - 1

    let recordUpdate data item =
        let newData = data.Data[0 .. data.Cursor - 1] @ [item] @ data.Data[data.Cursor + 1 .. data.Data.Length - 1]
        { data with Data = newData }

    let recordInsert data item =
        let newData = data.Data[0 .. data.Cursor - 1] @ [item] @ data.Data[data.Cursor .. data.Data.Length - 1]
        { data with Data = newData }

    let create viewSize dataSize = {
            Top = -1
            Bottom = -1
            Cursor = -1
            ViewSize = viewSize
            DataSize = dataSize
            Data = []
            Lasterror = None
        }   

[<RequireQualifiedAccess>]
type State =
    | Browsing
    | Adding
    | Editing
    | Saving
    | Shifting

type AgendaAction = {
    Key: {| Shortcut: string; Alt: bool |}           // "F2", "F3", etc.
    Label: string         // "Edit", "View", etc.
    Handler: unit -> unit
    Enabled: bool
}

[<ReactComponent>]
let WgAgendaButton (action: AgendaAction) =
    let altPrefix = if action.Key.Alt then "Alt+" else ""
    let keyLabel = if action.Key.Shortcut.Length > 0 then $" ({altPrefix}{action.Key.Shortcut})" else ""
    let buttonText = $"{action.Label}{keyLabel}"
    
    Html.xbutton [
        prop.buttonText buttonText
        prop.onClick (fun _ -> if action.Enabled then action.Handler())
        prop.disabled (not action.Enabled)
        prop.classes [ "commander-button" ]
    ]


[<ReactComponent>]
let WgAgendaEdit (isOpen: bool) (onSave: unit -> unit) (onClose: unit -> unit) (title: string) (children: ReactElement) =
    if isOpen then
        let modalRoot = document.getElementById("modal-root")
        ReactDOM.createPortal(
            Html.div [
                prop.className "modal-overlay"
                prop.onClick (fun _ -> onClose())
                prop.children [
                    Html.div [
                        prop.className "modal-content"
                        prop.onClick (fun ev -> ev.stopPropagation())
                        prop.children [
                            Html.div [
                                prop.className "modal-header"
                                prop.children [
                                    Html.h3 [
                                        prop.className "modal-title"
                                        prop.text title
                                    ]
                                    Html.button [
                                        prop.className "modal-close"
                                        prop.text "X"
                                        prop.onClick (fun _ -> onClose())
                                    ]
                                ]
                            ]
                            Html.div [
                                prop.className "modal-body"
                                prop.children [ children ]
                            ]
                            Html.div [
                                prop.className "modal-footer"
                                prop.children [
                                    Html.button [
                                        prop.className "modal-button modal-button-secondary"
                                        prop.text "cancel"
                                        prop.onClick (fun _ -> onClose())
                                    ]
                                    Html.button [
                                        prop.className "modal-button modal-button-primary"
                                        prop.text "save"
                                        prop.onClick (fun _ -> onSave())
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ],
            modalRoot
        )
    else
        Html.none

[<ReactComponent>]
let WgAgenda (props:{|
        Structure: WgListStructure<'T>
        useEditor:'T -> list<Field> * ( unit ->  'T)
        ItemNew: unit -> 'T
        ItemSave: 'T -> Async<Result<'T, string>>
        FetchBefore: Option<'T> -> int -> Async<Result<list<'T>, string>>
        FetchAfter: Option<'T> -> int -> Async<Result<list<'T>, string>>
    |}) =


    let (buffer, setBuffer) = React.useState(GridBuffer.create 15 100)
    let view = GridBuffer.view buffer
    let (deltaMove, setDeltaMove) = React.useState(1)
    let (state, setState) = React.useState(State.Browsing)
    let (lastError, setLastError) = React.useState(None)

    let defaultItem = props.ItemNew()
    let currentItem = 
        match state with
        | State.Adding -> defaultItem
        | State.Editing when GridBuffer.cursorValid buffer -> buffer.Data[buffer.Cursor]
        | _ -> defaultItem
    // hook call - must be unconditional like any other hook 
    let (fields, getUpdatedItem) = props.useEditor currentItem

    React.useEffect((fun () -> (
        async {
            if state = State.Browsing && deltaMove <> 0 then
                let! newBuffer = GridBuffer.move buffer deltaMove props.FetchBefore props.FetchAfter
                setBuffer newBuffer
                setDeltaMove 0
                setLastError newBuffer.Lasterror
        } |> Async.StartImmediate
        )), [| box deltaMove |])


    let handleSave =
        React.useDeferredCallback(props.ItemSave, 
            (fun x ->
                setLastError None
                match x with
                | Deferred.HasNotStartedYet -> setState State.Browsing
                | Deferred.InProgress -> setState State.Saving
                | Deferred.Failed exn -> 
                    setState State.Browsing
                    setLastError (Some exn.Message)
                | Deferred.Resolved content ->
                    match content with
                        | Ok energy ->
                            match state with
                            | State.Adding ->
                                let newBuffer = GridBuffer.recordInsert buffer energy
                                setBuffer newBuffer
                            | State.Editing ->
                                let newBuffer = GridBuffer.recordUpdate buffer energy
                                setBuffer newBuffer
                            | _ -> ()
                        | Error error -> 
                            setLastError (Some error)
                    setState(State.Browsing)
            )
        )


    let handleCancel () =
        setState(State.Browsing)


    let renderError () =
        match lastError with
        | Some error -> Html.text error
        | None -> Html.none

    let listRows = view |> List.map (fun item ->
            let row = props.Structure.Headers |> List.map (fun header ->
                header.DataGetter item
            )
            props.Structure.IdGetter(item), row
    )

    let listProps = {|
            Structure = props.Structure
            Rows = listRows
            RowCount = buffer.ViewSize
            Cursor = buffer.Cursor - buffer.Top
        |}

    let editArea =
        match state with
        | State.Adding | State.Editing when GridBuffer.cursorValid buffer ->
            let onSave () = handleSave(getUpdatedItem())
            WgAgendaEdit 
                true 
                onSave
                handleCancel 
                (if state = State.Adding then "Add New Item" else "Edit Item")
                (WgEditFields fields)
                
        | State.Editing -> 
            Html.text $"invalid cursor: {buffer.Cursor}"
            
        | _ -> Html.none

    let actions: AgendaAction list = [
        { Key = {| Shortcut = "PageUp"; Alt = false |}; Label = "PgUp"; Handler = (fun () -> if state = State.Browsing then setDeltaMove(buffer.ViewSize * -1)); Enabled = true }
        { Key = {| Shortcut = "PageDown"; Alt = false |}; Label = "PgDown"; Handler = (fun () -> if state = State.Browsing then setDeltaMove(buffer.ViewSize)); Enabled = true }
        { Key = {| Shortcut = "ArrowUp"; Alt = false |}; Label = "Up"; Handler = (fun () -> if state = State.Browsing then setDeltaMove(-1)); Enabled = true }
        { Key = {| Shortcut = "ArrowDown"; Alt = false |}; Label = "Down"; Handler = (fun () -> if state = State.Browsing then setDeltaMove(1)); Enabled = true }
        { Key = {| Shortcut = "n"; Alt = true |}; Label = "Add"; Handler = (fun () -> if state = State.Browsing then setState(State.Adding)); Enabled = state = State.Browsing }
        { Key = {| Shortcut = "e"; Alt = true |}; Label = "Edit"; Handler = (fun () -> if state = State.Browsing && GridBuffer.cursorValid buffer then setState(State.Editing)); Enabled = state = State.Browsing && GridBuffer.cursorValid buffer }
    ]


    // Global keyboard handler for function keys
    React.useListener.onKeyDown(fun ev ->
        // Only handle function keys
        let matchingAction = 
            actions 
            |> List.tryFind (fun action -> action.Key.Shortcut = ev.key && action.Key.Alt = ev.altKey && action.Enabled)
        
        match matchingAction with
        | Some action -> 
            ev.preventDefault()
            action.Handler()
        | None -> ()
    )

    let buttonBar = 
        Html.div [
            prop.classes [ "commander-button-bar" ]
            prop.children (
                actions 
                |> List.map WgAgendaButton
            )
        ]

    let contentArea = 
        Html.div [
            // prop.classes [ "agenda-content"; "agenda-overlay" ]
            prop.children [
                Html.div [
                    // prop.classes [ "commander-list-panel" ]
                    prop.id "edit-portal"
                    prop.children [
                        WgList listProps
                    ]                
                ]
                editArea
            ]
        ]


    Html.div [
        renderError ()
        contentArea
        buttonBar
    ]
