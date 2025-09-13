module WgCommander

open Feliz
open Feliz.UseListener
open CustomElements

type CommanderAction = {
    Key: string           // "F2", "F3", etc.
    Label: string         // "Edit", "View", etc.
    Handler: unit -> unit
    Enabled: bool
}

type PanelLayout = 
    | Single                    // Just list
    | SplitRight               // List | Edit
    | SplitLeft                // Edit | List  
    | SplitTop                 // Edit above List
    | SplitBottom              // List above Edit
    | Overlay                  // Edit overlays List (mobile)

type CommanderConfig = {
    Title: string
    Actions: CommanderAction list
    ShowShortcuts: bool
    Layout: PanelLayout
    ShowEditPanel: bool
}

[<ReactComponent>]
let WgCommanderButton (action: CommanderAction) =
    let keyLabel = if action.Key.Length > 0 then $"{action.Key} " else ""
    let buttonText = $"{keyLabel}{action.Label}"
    
    Html.xbutton [
        prop.buttonText buttonText
        prop.onClick (fun _ -> if action.Enabled then action.Handler())
        prop.disabled (not action.Enabled)
        prop.classes [ "commander-button" ]
    ]

[<ReactComponent>]
let WgCommanderPanel (config: CommanderConfig) (listContent: ReactElement) (editContent: ReactElement option) =
    
    // Global keyboard handler for function keys
    React.useListener.onKeyDown(fun ev ->
        // Only handle function keys
        if ev.key.StartsWith("F") then
            let matchingAction = 
                config.Actions 
                |> List.tryFind (fun action -> action.Key = ev.key && action.Enabled)
            
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
                config.Actions 
                |> List.map WgCommanderButton
            )
        ]
    
    let layoutClass = 
        match config.Layout with
        | Single -> "commander-single"
        | SplitRight -> "commander-split-right"
        | SplitLeft -> "commander-split-left"
        | SplitTop -> "commander-split-top"
        | SplitBottom -> "commander-split-bottom"
        | Overlay -> "commander-overlay"
    
    let listPanel = 
        Html.div [
            prop.classes [ "commander-list-panel" ]
            prop.children [ listContent ]
        ]
    
    let editPanel = 
        match editContent with
        | Some content when config.ShowEditPanel ->
            Html.div [
                prop.classes [ "commander-edit-panel" ]
                prop.children [
                    // Close button for edit panel
                    Html.div [
                        prop.classes [ "commander-edit-header" ]
                        prop.children [
                            Html.text "Edit Record"
                            Html.xbutton [
                                prop.buttonText "✕"
                                prop.classes [ "commander-close-btn" ]
                                prop.onClick (fun _ -> 
                                    // This should be handled by parent component
                                    Browser.Dom.console.log "Close edit panel"
                                )
                            ]
                        ]
                    ]
                    Html.div [
                        prop.classes [ "commander-edit-content" ]
                        prop.children [ content ]
                    ]
                ]
            ]
        | _ -> Html.none
    
    let contentArea = 
        Html.div [
            prop.classes [ "commander-content"; layoutClass ]
            prop.children [
                listPanel
                editPanel
            ]
        ]
    
    Html.div [
        prop.classes [ "commander-panel" ]
        prop.children [
            // Title bar (optional)
            if config.Title.Length > 0 then
                Html.div [
                    prop.classes [ "commander-title" ]
                    prop.children [ Html.text config.Title ]
                ]
            
            // Main content area with layout
            contentArea
            
            // Button bar at bottom
            buttonBar
        ]
    ]