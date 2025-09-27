module WgAgenda

open Feliz
open Feliz.UseDeferred
open Feliz.UseListener
open Browser.Dom
open WgList
open WgEdit
open CustomElements

module FocusManager =
    
    let findCustomElements (modalElement: Browser.Types.Element) =
        modalElement.querySelectorAll("x-date, x-select, x-number, x-input")
        |> fun nodeList -> [| for i in 0 .. nodeList.length - 1 -> nodeList.item(i) |]
        |> Array.choose (fun node -> 
            match node with 
            | :? Browser.Types.HTMLElement as elem -> Some elem
            | _ -> None)
        |> Array.toList

    let findOtherFocusableElements (modalElement: Browser.Types.Element) =
        modalElement.querySelectorAll("button, [href], input, select, textarea, [tabindex]:not([tabindex='-1'])")
        |> fun nodeList -> [| for i in 0 .. nodeList.length - 1 -> nodeList.item(i) |]
        |> Array.choose (fun node -> 
            match node with 
            | :? Browser.Types.HTMLElement as elem -> 
                // Only include if not already a custom element
                let tagName = elem.tagName.ToLower()
                if not (tagName.StartsWith("x-")) then Some elem else None
            | _ -> None)
        |> Array.toList

    let getAllFocusableElements (modalElement: Browser.Types.Element) =
        let customElements = findCustomElements modalElement
        let otherElements = findOtherFocusableElements modalElement
        customElements @ otherElements

    let isSameElement (activeElement: Browser.Types.Element) (targetElement: Browser.Types.HTMLElement) =
        // Use the DOM's isSameNode method for reliable element comparison
        activeElement.isSameNode(targetElement :> Browser.Types.Node)

    let handleTabNavigation (modalRef: Fable.React.IRefValue<Browser.Types.Element option>) (keyEvent: Browser.Types.KeyboardEvent) =
        match modalRef.current with
        | Some modalElement ->
            let focusableElements = getAllFocusableElements modalElement

            match focusableElements with
            | firstElement :: _ ->
                let lastElement = List.last focusableElements
                let activeElement = document.activeElement

                if keyEvent.shiftKey then
                    // Shift+Tab: if on first element or outside modal, go to last
                    if isSameElement activeElement firstElement || not (modalElement.contains(activeElement)) then
                        keyEvent.preventDefault()
                        lastElement.focus()
                else
                    // Tab: if on last element or outside modal, go to first  
                    if isSameElement activeElement lastElement || not (modalElement.contains(activeElement)) then
                        keyEvent.preventDefault()
                        firstElement.focus()
            | [] -> 
                // No focusable elements - prevent Tab from doing anything
                keyEvent.preventDefault()
        | None -> ()

    let setInitialFocus (modalRef: Fable.React.IRefValue<Browser.Types.Element option>) =
        match modalRef.current with
        | Some modalElement ->
            let customElements = findCustomElements modalElement
            
            match customElements with
            | firstCustom :: _ -> 
                // Focus the first custom element
                firstCustom.focus()
            | [] ->
                // Fallback: look for standard form elements
                let standardElements = findOtherFocusableElements modalElement
                
                match standardElements with
                | first :: _ -> first.focus()
                | [] -> ()
        | None -> ()

    let createKeyDownHandler (modalRef: Fable.React.IRefValue<Browser.Types.Element option>) (onClose: unit -> unit) =
        fun (e: Browser.Types.Event) ->
            let keyEvent = e :?> Browser.Types.KeyboardEvent
            if keyEvent.key = "Tab" then
                handleTabNavigation modalRef keyEvent
            elif keyEvent.key = "Escape" then
                // Handle Escape key to close modal
                keyEvent.preventDefault()
                onClose()

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
let WgAgendaEdit (onSave: unit -> unit) (onClose: unit -> unit) (title: string) (children: ReactElement) =
    let modalRef = React.useRef<Browser.Types.Element option>(None)

    // Focus trap effect
    React.useEffect((fun () ->
        // Create and register the keyboard handler
        let handleKeyDown = FocusManager.createKeyDownHandler modalRef onClose
        document.addEventListener("keydown", handleKeyDown)
        
        // Set initial focus to first custom element
        let timeoutId = window.setTimeout((fun () ->
            FocusManager.setInitialFocus modalRef
        ), 100, [||])

        // Return cleanup function as IDisposable
        { new System.IDisposable with
            member _.Dispose() = 
                document.removeEventListener("keydown", handleKeyDown)
                window.clearTimeout(timeoutId) }
    ), [||])

    let modalRoot = document.getElementById("modal-root")
    ReactDOM.createPortal(
            Html.div [
                prop.className "modal-overlay"
                prop.onClick (fun _ -> onClose())
                prop.children [
                    Html.div [
                        prop.className "modal-content"
                        prop.onClick (fun ev -> ev.stopPropagation())
                        prop.ref (fun elem -> modalRef.current <- Some elem)
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

[<ReactComponent>]
let WgAgenda (props:{|
        Structure: WgListStructure<'T>
        useEditor:'T -> list<Field> * ( unit ->  'T)
        ItemNew: unit -> 'T
        ItemSave: 'T -> Async<Result<'T, string>>
        FetchBefore: Option<'T> -> int -> Async<Result<list<'T>, string>>
        FetchAfter: Option<'T> -> int -> Async<Result<list<'T>, string>>
    |}) =

    // ===== HOOKS (Root Level) =====
    let buffer, setBuffer = React.useState(GridBuffer.create 15 100)
    let deltaMove, setDeltaMove = React.useState(1)
    let state, setState = React.useState(State.Browsing)
    let lastError, setLastError = React.useState(None)

    let defaultItem = React.useMemo((fun () -> props.ItemNew()), [| |])
    let currentItem = 
        match state with
        | State.Adding -> defaultItem
        | State.Editing when GridBuffer.cursorValid buffer -> buffer.Data[buffer.Cursor]
        | _ -> defaultItem
    
    let fields, getUpdatedItem = props.useEditor currentItem

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

    React.useEffect((fun () ->
        async {
            if state = State.Browsing && deltaMove <> 0 then
                let! newBuffer = GridBuffer.move buffer deltaMove props.FetchBefore props.FetchAfter
                setBuffer newBuffer
                setDeltaMove 0
                setLastError newBuffer.Lasterror
        } |> Async.StartImmediate
        ), [| box deltaMove |])

    let actions: AgendaAction list = [
        { Key = {| Shortcut = "PageUp"; Alt = false |}; Label = "PgUp"; Handler = (fun () -> if state = State.Browsing then setDeltaMove(buffer.ViewSize * -1)); Enabled = true }
        { Key = {| Shortcut = "PageDown"; Alt = false |}; Label = "PgDown"; Handler = (fun () -> if state = State.Browsing then setDeltaMove(buffer.ViewSize)); Enabled = true }
        { Key = {| Shortcut = "ArrowUp"; Alt = false |}; Label = "Up"; Handler = (fun () -> if state = State.Browsing then setDeltaMove(-1)); Enabled = true }
        { Key = {| Shortcut = "ArrowDown"; Alt = false |}; Label = "Down"; Handler = (fun () -> if state = State.Browsing then setDeltaMove(1)); Enabled = true }
        { Key = {| Shortcut = "n"; Alt = true |}; Label = "Add"; Handler = (fun () -> if state = State.Browsing then setState(State.Adding)); Enabled = state = State.Browsing }
        { Key = {| Shortcut = "e"; Alt = true |}; Label = "Edit"; Handler = (fun () -> if state = State.Browsing && GridBuffer.cursorValid buffer then setState(State.Editing)); Enabled = state = State.Browsing && GridBuffer.cursorValid buffer }
    ]

    React.useListener.onKeyDown(fun ev ->
        let matchingAction = 
            actions 
            |> List.tryFind (fun action -> action.Key.Shortcut = ev.key && action.Key.Alt = ev.altKey && action.Enabled)
        
        match matchingAction with
        | Some action -> 
            ev.preventDefault()
            action.Handler()
        | None -> ()
    )

    // ===== EDIT AREA =====
    let editArea =
        match state with
        | State.Adding | State.Editing when GridBuffer.cursorValid buffer ->
            let onSave () = handleSave(getUpdatedItem())
            let onCancel () = setState(State.Browsing)
            WgAgendaEdit 
                onSave
                onCancel 
                (if state = State.Adding then "Add New Item" else "Edit Item")
                (WgEditFields fields)
                
        | State.Editing -> 
            Html.text $"invalid cursor: {buffer.Cursor}"
            
        | _ -> Html.none

    // ===== LIST CONTENT =====
    let view = GridBuffer.view buffer
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

    let listContent = 
        Html.div [
            prop.id "edit-portal"
            prop.children [
                WgList listProps
            ]                
        ]

    // ===== UI COMPONENTS =====
    let errorDisplay =
        match lastError with
        | Some error -> Html.text error
        | None -> Html.none

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
            prop.children [
                listContent
                editArea
            ]
        ]

    // ===== RENDER =====
    Html.div [
        errorDisplay
        contentArea
        buttonBar
    ]
