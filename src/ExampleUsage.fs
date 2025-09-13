// Complete example usage with edit panel
module ExampleUsage

open WgCommander
open WgList
open WgEdit
open Feliz

[<ReactComponent>]
let MyDataManager() =
    let selectedItem, setSelectedItem = React.useState("")
    let isEditing, setIsEditing = React.useState(false)
    let editingItem, setEditingItem = React.useState(None)
    let currentLayout, setCurrentLayout = React.useState(SplitRight)
    
    // Sample data
    let sampleData = [
        ("item1", ["John Doe"; "30"; "Developer"])
        ("item2", ["Jane Smith"; "25"; "Designer"]) 
        ("item3", ["Bob Johnson"; "35"; "Manager"])
    ]
    
    // Edit form fields (example using WgEdit)
    let editForm = 
        match editingItem with
        | Some item ->
            let nameField: StrField = { Name = "Name"; Value = "John Doe"; HandleChange = fun v -> Browser.Dom.console.log $"Name: {v}" }
            let ageField: IntField = { Name = "Age"; Value = 30; HandleChange = fun v -> Browser.Dom.console.log $"Age: {v}" }
            let roleField: StrField = { Name = "Role"; Value = "Developer"; HandleChange = fun v -> Browser.Dom.console.log $"Role: {v}" }
            
            let fields = [
                StrField nameField
                IntField ageField  
                StrField roleField
            ]
            
            Some (WgEdit fields (fun () -> setIsEditing(false)) (fun () -> setIsEditing(false)))
        | None -> None
    
    // Commander configuration
    let commanderConfig = {
        Title = "Data Manager"
        ShowShortcuts = true
        Layout = currentLayout
        ShowEditPanel = isEditing
        Actions = [
            { Key = "F1"; Label = "Help"; Handler = (fun () -> Browser.Dom.console.log "Help"); Enabled = true }
            { Key = "F2"; Label = "Edit"; Handler = (fun () -> setIsEditing(true); setEditingItem(Some selectedItem)); Enabled = (selectedItem <> "") }
            { Key = "F3"; Label = "View"; Handler = (fun () -> Browser.Dom.console.log "View"); Enabled = (selectedItem <> "") }
            { Key = "F4"; Label = "New"; Handler = (fun () -> setIsEditing(true); setEditingItem(None)); Enabled = true }
            { Key = "F6"; Label = "Layout"; Handler = (fun () -> 
                let nextLayout = 
                    match currentLayout with
                    | SplitRight -> SplitLeft
                    | SplitLeft -> SplitTop  
                    | SplitTop -> SplitBottom
                    | SplitBottom -> Overlay
                    | Overlay -> SplitRight
                    | Single -> SplitRight
                setCurrentLayout(nextLayout)
            ); Enabled = true }
            { Key = "F8"; Label = "Delete"; Handler = (fun () -> Browser.Dom.console.log "Delete"); Enabled = (selectedItem <> "") }
            { Key = "F10"; Label = "Exit"; Handler = (fun () -> setIsEditing(false)); Enabled = isEditing }
        ]
    }
    
    // List structure
    let listStructure = {
        Headers = [
            { Label = "Name"; FlexBasis = 40; DataGetter = fun f -> f }
            { Label = "Age"; FlexBasis = 20; DataGetter = fun f -> f }
            { Label = "Role"; FlexBasis = 40; DataGetter = fun f -> f }
        ]
        IdGetter = fun f -> f
    }
    
    // Create the list
    let dataList = WgList {|
        Structure = listStructure
        Rows = sampleData
        RowCount = 10
        Cursor = 0
    |}
    
    // Wrap in commander panel
    WgCommanderPanel commanderConfig dataList editForm

// Layout showcase
[<ReactComponent>]
let LayoutShowcase() =
    let currentLayout, setCurrentLayout = React.useState(SplitRight)
    
    let sampleList = Html.div [
        prop.style [ style.padding 20 ]
        prop.children [
            Html.h3 "Sample List"
            Html.div "Item 1"
            Html.div "Item 2" 
            Html.div "Item 3"
        ]
    ]
    
    let sampleEdit = Html.div [
        prop.style [ style.padding 20 ]
        prop.children [
            Html.h3 "Edit Form"
            Html.div "Name: [Input]"
            Html.div "Age: [Input]"
            Html.div "Role: [Input]"
        ]
    ]
    
    let config = {
        Title = $"Layout: {currentLayout}"
        ShowShortcuts = true  
        Layout = currentLayout
        ShowEditPanel = true
        Actions = [
            { Key = "F1"; Label = "Right"; Handler = (fun () -> setCurrentLayout(SplitRight)); Enabled = true }
            { Key = "F2"; Label = "Left"; Handler = (fun () -> setCurrentLayout(SplitLeft)); Enabled = true }
            { Key = "F3"; Label = "Top"; Handler = (fun () -> setCurrentLayout(SplitTop)); Enabled = true }
            { Key = "F4"; Label = "Bottom"; Handler = (fun () -> setCurrentLayout(SplitBottom)); Enabled = true }
            { Key = "F5"; Label = "Overlay"; Handler = (fun () -> setCurrentLayout(Overlay)); Enabled = true }
        ]
    }
    
    WgCommanderPanel config sampleList (Some sampleEdit)