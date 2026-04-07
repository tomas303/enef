module WgAgenda

open Feliz
open Feliz.UseDeferred
open Feliz.UseListener
open Browser.Dom
open WgList
open WgEdit
open CustomElements
open Lib
open Fable.Core
open Fable.Core.JsInterop

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

    type Config<'T> = {
        ViewSize: int
        DataSize: int
        FetchBefore: Option<'T> -> int -> Async<Result<list<'T>, string>>
        FetchAfter: Option<'T> -> int -> Async<Result<list<'T>, string>>
    }

    type State<'T> = {
        Top: int
        Bottom: int
        Cursor: int
        Data: List<'T>
        Lasterror: Option<string>
    }

    let private applyDelta (config: Config<'T>) (state: State<'T>) delta =
        let newCursor = state.Cursor + delta
        if newCursor < state.Top || newCursor > state.Bottom then
            let newTop = state.Top + delta
            { state with
                Cursor = newCursor
                Top = newTop
                Bottom = newTop + config.ViewSize - 1
            }
        else { state with Cursor = newCursor }

    let private readBefore (config: Config<'T>) (state: State<'T>) = async {
        let delta = abs(state.Top)
        let! fetchData =
            if state.Data.Length > 0
            then config.FetchBefore (Some state.Data[0]) delta
            else config.FetchBefore None delta
        match fetchData with
        | Ok content ->
            let newData = content @ state.Data
            let newData =
                if newData.Length > config.DataSize then
                    List.take config.DataSize newData
                else
                    newData
            return
                { state with
                    Cursor = state.Cursor + content.Length
                    Top = state.Top + content.Length
                    Bottom = state.Bottom + content.Length
                    Data = newData }
        | Error error ->
            return { state with Lasterror = Some(error) }
    }

    let private readAfter (config: Config<'T>) (state: State<'T>) = async {
        let delta = max config.ViewSize (abs(state.Bottom - state.Data.Length))
        let! fetchData =
            if state.Data.Length > 0
            then config.FetchAfter (Some state.Data[state.Data.Length - 1]) delta
            else config.FetchAfter None delta
        match fetchData with
        | Ok content ->
            let newData = state.Data @ content
            let removeCnt =
                if newData.Length > config.DataSize
                then newData.Length - config.DataSize
                else 0
            return
                { state with
                    Cursor = state.Cursor - removeCnt
                    Top = state.Top - removeCnt
                    Bottom = state.Bottom - removeCnt
                    Data = List.skip removeCnt newData }
        | Error error ->
            return { state with Lasterror = Some(error) }
    }

    let private correct (config: Config<'T>) (state: State<'T>) =
        let newTop =
            if state.Top < 0 then
                0
            else if state.Top > state.Data.Length - 1 then
                state.Data.Length - 1
            else
                state.Top
        let newBottom =
            if newTop + config.ViewSize - 1 > state.Data.Length - 1
            then state.Data.Length - 1
            else newTop + config.ViewSize - 1
        let newCursor =
            if state.Cursor < newTop
            then newTop
            else if state.Cursor > newBottom
            then newBottom
            else state.Cursor
        { state with
            Cursor = newCursor
            Top = newTop
            Bottom = newBottom }

    let private refetchAroundItem (config: Config<'T>) (item: 'T) = async {
        let! beforeResult = config.FetchBefore (Some item) config.ViewSize
        let! afterResult = config.FetchAfter (Some item) config.ViewSize
        match beforeResult, afterResult with
        | Ok before, Ok after ->
            let newData = before @ [item] @ after
            let newData =
                if newData.Length > config.DataSize
                then List.take config.DataSize newData
                else newData
            let cursor = before.Length
            let top = max 0 (cursor - config.ViewSize)
            let bottom = min (newData.Length - 1) (top + config.ViewSize - 1)
            return Ok { Top = top; Bottom = bottom; Cursor = cursor; Data = newData; Lasterror = None }
        | Error e, _ | _, Error e ->
            return Error e
    }

    let move (config: Config<'T>) (state: State<'T>) delta = async {
        let newState = applyDelta config state delta
        Dbg.wl $"move: delta={delta} => cursor={newState.Cursor}, top={newState.Top}, bottom={newState.Bottom}"
        let! newState =
            if newState.Top < 0 then
                Dbg.wl "reading before"
                readBefore config newState
            else if newState.Bottom > newState.Data.Length - 1 then
                Dbg.wl "reading after"
                readAfter config newState
            else
                Dbg.wl "no read needed"
                async { return newState }
        let newState = correct config newState
        return newState
    }

    let view (state: State<'T>) =
        state.Data[state.Top..state.Bottom]

    let cursorValid (state: State<'T>) =
        state.Cursor >= 0 && state.Cursor <= state.Data.Length - 1

    let recordUpdate (config: Config<'T>) (item: 'T) =
        refetchAroundItem config item

    let recordInsert (config: Config<'T>) (item: 'T) =
        refetchAroundItem config item

    let createConfig viewSize dataSize fetchBefore fetchAfter : Config<'T> = {
        ViewSize = viewSize
        DataSize = dataSize
        FetchBefore = fetchBefore
        FetchAfter = fetchAfter
    }

    let createState<'T> () : State<'T> = {
        Top = -1
        Bottom = -1
        Cursor = -1
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
        prop.key action.Label  // ← For React (invisible)
        prop.custom("data-action", action.Label)  // ← For debugging (visible)
        prop.custom("data-actionenabled", action.Enabled)  // ← For debugging (visible)
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
let private WgEditorPanel (props: {|
        key: string
        item: 'T
        useEditor: 'T -> list<Field> * (unit -> 'T)
        onSave: 'T -> unit
        onCancel: unit -> unit
        title: string |}) =
    let fields, getUpdatedItem = props.useEditor props.item
    WgAgendaEdit (fun () -> props.onSave(getUpdatedItem())) props.onCancel props.title (WgEditFields fields)

[<Emit("new ResizeObserver($0)")>]
let private createResizeObserver (callback: obj[] -> unit) : obj = jsNative

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
    let rowHeightPx = 32.0
    let isInitialized = React.useRef(false)
    let rowHeightCalibrated = React.useRef(false)
    let viewSize, setViewSize = React.useState(0)  // 0 = unknown until ResizeObserver fires
    let listContainerRef = React.useRef<Browser.Types.Element option>(None)
    let bufferConfig = React.useMemo((fun () -> GridBuffer.createConfig viewSize 100 props.FetchBefore props.FetchAfter), [| box viewSize |])
    let buffer, setBuffer = React.useState(GridBuffer.createState())
    let state, setState = React.useState State.Browsing
    let lastError, setLastError = React.useState(None)

    let defaultItem = React.useMemo((fun () -> props.ItemNew()), [| |])
    let currentItem = 
        match state with
        | State.Adding -> defaultItem
        | State.Editing when GridBuffer.cursorValid buffer -> buffer.Data[buffer.Cursor]
        | _ -> defaultItem

    let handleMove =
        React.useDeferredCallback(
            (fun delta -> GridBuffer.move bufferConfig buffer delta),
            (fun x ->
                match x with
                | Deferred.HasNotStartedYet -> ()
                | Deferred.InProgress -> setState State.Shifting
                | Deferred.Failed exn -> 
                    setState State.Browsing
                    setLastError (Some exn.Message)
                | Deferred.Resolved newBuffer ->
                    setBuffer newBuffer
                    setLastError newBuffer.Lasterror
                    setState State.Browsing
            )
        )

    let handleSave =
        React.useDeferredCallback(
            (fun (item, wasAdding) -> async {
                let! saveResult = props.ItemSave item
                match saveResult with
                | Ok savedItem ->
                    return!
                        if wasAdding
                        then GridBuffer.recordInsert bufferConfig savedItem
                        else GridBuffer.recordUpdate bufferConfig savedItem
                | Error e -> return Error e
            }),
            (fun x ->
                setLastError None
                match x with
                | Deferred.HasNotStartedYet -> setState State.Browsing
                | Deferred.InProgress -> setState State.Saving
                | Deferred.Failed exn ->
                    setState State.Browsing
                    setLastError (Some exn.Message)
                | Deferred.Resolved result ->
                    match result with
                    | Ok newBuffer ->
                        setBuffer newBuffer
                        setLastError newBuffer.Lasterror
                    | Error error ->
                        setLastError (Some error)
                    setState State.Browsing
            )
        )

    // Observe list container height and recalculate viewSize
    React.useEffect((fun () ->
        match listContainerRef.current with
        | None -> { new System.IDisposable with member _.Dispose() = () }
        | Some el ->
            let observer = createResizeObserver (fun entries ->
                entries |> Array.iter (fun entry ->
                    if entry?target = (el :> obj) then
                        let height: float = entry?contentRect?height
                        let firstRow = el.querySelector(".fg-row")
                        let rowH =
                            if isNull firstRow then rowHeightPx
                            else
                                let rect = firstRow.getBoundingClientRect()
                                if rect.height > 0.0 then rect.height else rowHeightPx
                        let rows = max 5 (int (height / rowH))
                        setViewSize rows
                )
            )
            observer?observe(el)
            { new System.IDisposable with member _.Dispose() = observer?disconnect() }
    ), [||])

    // Init on first known viewSize; re-sync when viewSize changes after data is loaded
    React.useEffect((fun () ->
        if viewSize > 0 then
            if GridBuffer.cursorValid buffer then handleMove(0)
            elif not isInitialized.current then
                isInitialized.current <- true
                handleMove(1)
    ), [| box viewSize |])

    // One-time recalibration: after first data load measure real row height and correct viewSize
    React.useEffect((fun () ->
        if GridBuffer.cursorValid buffer && not rowHeightCalibrated.current then
            rowHeightCalibrated.current <- true
            match listContainerRef.current with
            | None -> ()
            | Some el ->
                let firstRow = el.querySelector(".fg-row")
                if not (isNull firstRow) then
                    let rowRect = firstRow.getBoundingClientRect()
                    if rowRect.height > 0.0 then
                        let containerHeight = el.getBoundingClientRect().height
                        let calibrated = max 5 (int (containerHeight / rowRect.height))
                        if calibrated <> viewSize then setViewSize calibrated
    ), [| box buffer |])

    let actions: AgendaAction list = [
        { Key = {| Shortcut = "PageUp"; Alt = false |}; Label = "PgUp"; Handler = (fun () -> if state = State.Browsing then handleMove(bufferConfig.ViewSize * -1)); Enabled = state = State.Browsing }
        { Key = {| Shortcut = "PageDown"; Alt = false |}; Label = "PgDown"; Handler = (fun () -> if state = State.Browsing then handleMove(bufferConfig.ViewSize)); Enabled = state = State.Browsing }
        { Key = {| Shortcut = "ArrowUp"; Alt = false |}; Label = "Up"; Handler = (fun () -> if state = State.Browsing then handleMove(-1)); Enabled = state = State.Browsing }
        { Key = {| Shortcut = "ArrowDown"; Alt = false |}; Label = "Down"; Handler = (fun () -> if state = State.Browsing then handleMove(1)); Enabled = state = State.Browsing }
        { Key = {| Shortcut = "n"; Alt = true |}; Label = "Add"; Handler = (fun () -> if state = State.Browsing then setState State.Adding); Enabled = state = State.Browsing }
        { Key = {| Shortcut = "e"; Alt = true |}; Label = "Edit"; Handler = (fun () -> if state = State.Browsing && GridBuffer.cursorValid buffer then setState State.Editing); Enabled = state = State.Browsing && GridBuffer.cursorValid buffer }
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
        | State.Adding ->
            WgEditorPanel {|
                key = "add"
                item = currentItem
                useEditor = props.useEditor
                onSave = fun item -> handleSave (item, true)
                onCancel = fun () -> setState State.Browsing
                title = "Add New Item" |}
        | State.Editing when GridBuffer.cursorValid buffer ->
            WgEditorPanel {|
                key = props.Structure.IdGetter currentItem
                item = currentItem
                useEditor = props.useEditor
                onSave = fun item -> handleSave (item, false)
                onCancel = fun () -> setState State.Browsing
                title = "Edit Item" |}
        | State.Editing -> 
            Html.text $"invalid cursor: {buffer.Cursor}"
        | _ -> 
            Html.none

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
        RowCount = bufferConfig.ViewSize
        Cursor = buffer.Cursor - buffer.Top
    |}

    let listContent = 
        Html.div [
            prop.id "edit-portal"
            prop.ref (fun el -> listContainerRef.current <- Some el)
            prop.style [ style.height (length.percent 100); style.overflow.hidden ]
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
                |> List.mapi (fun index action ->
                    Html.div [
                        prop.key $"action-{action.Label}"
                        prop.custom("data-button-wrapper", action.Label)  // ← Visible debug attribute
                        prop.children [ WgAgendaButton action ]
                    ]
                )
            )
        ]

    let contentArea = 
        Html.div [
            prop.style [ style.flexGrow 1; style.minHeight 0; style.overflow.hidden ]
            prop.children [
                listContent
                editArea
            ]
        ]

    // ===== RENDER =====
    Html.div [
        prop.style [ style.height (length.percent 100); style.display.flex; style.flexDirection.column ]
        prop.children [
            errorDisplay
            contentArea
            buttonBar
        ]
    ]
