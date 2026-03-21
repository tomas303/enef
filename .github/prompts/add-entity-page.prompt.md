---
description: "Add a new entity to the Feliz/F# app: extend Lib.fs with decode/encode/API functions, create a new PgXxx.fs page component, and wire it into App.fs (Page DU, menus list, page switch). Use when adding a new record type that needs a CRUD page. The type definition must already exist in Lib.fs."
name: "Add entity page"
argument-hint: "TypeName only, e.g. 'Customer'"
agent: "agent"
tools: [vscode/getProjectSetupInfo, vscode/installExtension, vscode/memory, vscode/newWorkspace, vscode/runCommand, vscode/vscodeAPI, vscode/extensions, vscode/askQuestions, execute/runNotebookCell, execute/testFailure, execute/getTerminalOutput, execute/awaitTerminal, execute/killTerminal, execute/createAndRunTask, execute/runInTerminal, execute/runTests, read/getNotebookSummary, read/problems, read/readFile, read/terminalSelection, read/terminalLastCommand, agent/runSubagent, edit/createDirectory, edit/createFile, edit/createJupyterNotebook, edit/editFiles, edit/editNotebook, edit/rename, search/changes, search/codebase, search/fileSearch, search/listDirectory, search/searchResults, search/textSearch, search/searchSubagent, search/usages, web/fetch, web/githubRepo, vscode.mermaid-chat-features/renderMermaidDiagram, todo]
---

Add a complete new entity page for the type: **$input**

The type record is already defined in [src/Lib.fs](../../src/Lib.fs). Discover its fields by reading that file — do not ask the user to describe them.

Follow all existing patterns precisely. The files to modify/create are:
- [src/Lib.fs](../../src/Lib.fs) — add Utils.new*, Encode.*, Decode.*, Api.* module
- `src/app.fsproj` — register the new file in compilation order
- `src/Pg$inputs.fs` — new file with editor hook + ReactComponent (filename and module are **plural**)
- [src/App.fs](../../src/App.fs) — extend Page DU, menus, and page switch

## Step 1 — Read existing files first

Read [src/Lib.fs](../../src/Lib.fs), [src/App.fs](../../src/App.fs), [src/Hooks.fs](../../src/Hooks.fs), and [src/app.fsproj](../../src/app.fsproj) in full before making any changes.

Locate the `$input` record type in Lib.fs. Inspect each field name and type to drive all decisions below.

**Naming convention used throughout:**
- Singular type name: `$input` (e.g. `Settlement`) — used for the record type, editor hook argument, and `Utils.new*`
- Plural name: `$inputs` (e.g. `Settlements`) — used for the **filename** (`Pg$inputs.fs`), **module name** (`module Pg$inputs`), and **ReactComponent function** (`Pg$inputs()`)

## Step 2 — Extend Lib.fs

Add the following sections **in order**, matching the exact indentation and style of adjacent entries.

### 2a. Utils.new* factory function

Add inside the `Utils` module, alongside `newProduct`, `newPlace`, etc.:

```fsharp
let new<TypeName> () = { ID = newID (); <field1> = <defaultValue1>; ... }
```

Default values by type:
- `string` field → `""`
- `int` field → `0`
- `int64` date field → `localDateTimeToUnixTime System.DateTime.Now`
- `float` field → `0.0`
- `EnergyKind` field → `EnergyKind.ElektricityNT`
- `PriceType` field → `PriceType.ComodityPerVolume`
- Foreign key `Xxx_ID: string` field → `""`

### 2b. Encode module function

Add inside the `Encode` module, after the last encode function:

```fsharp
let <typename> (x : <TypeName>) =
    Encode.object [
        "ID", Encode.string x.ID
        // for string fields:
        "<FieldName>", Encode.string x.<FieldName>
        // for int fields:
        "<FieldName>", Encode.int x.<FieldName>
        // for int64 fields (dates):
        "<FieldName>", Encode.int64 x.<FieldName>
        // for float fields:
        "<FieldName>", Encode.float x.<FieldName>
        // for EnergyKind fields:
        "<FieldName>", Encode.int (Constants.EnergyKindToInt.[x.<FieldName>])
        // for PriceType fields:
        "<FieldName>", Encode.int (Constants.PriceTypeToInt.[x.<FieldName>])
    ]
```

### 2c. Decode module function

Add inside the `Decode` module, after the last decode function.
If the type has `EnergyKind` or `PriceType` fields reuse the existing `energyKind` and `priceType` decoders.

```fsharp
let <typename> : Decoder<<TypeName>> =
    Decode.object (fun fields -> {
        ID = fields.Required.At [ "ID" ] Decode.string
        // string:
        <FieldName> = fields.Required.At [ "<FieldName>" ] Decode.string
        // int:
        <FieldName> = fields.Required.At [ "<FieldName>" ] Decode.int
        // int64:
        <FieldName> = fields.Required.At [ "<FieldName>" ] Decode.int64
        // float:
        <FieldName> = fields.Required.At [ "<FieldName>" ] Decode.float
        // EnergyKind:
        <FieldName> = fields.Required.At [ "<FieldName>" ] energyKind
        // PriceType:
        <FieldName> = fields.Required.At [ "<FieldName>" ] priceType
    })
```

### 2d. Api module

Add a new sub-module inside `Api`, after the last existing module.

Decide the **pagination key** based on the type:
- If sorted by name: use `name: string` + `id: string` → `?name={name}&id={id}&limit={limit}`
- If sorted by date: use `fromdate: int64` + `id: string` → `?fromdate={fromdate}&id={id}&limit={limit}`

Decide the **namespace** (REST path): lowercase plural of the type name, e.g. `customers`.

```fsharp
module <TypeNames> =
    let ns = "<typenames>"
    let loadAll () =
        get $"{url}/{ns}" (Decode.list Decode.<typename>)
    let saveItem (item : <TypeName>) = async {
        let json = Encode.<typename> item
        let body = Encode.toString 2 json
        let! (status, responseText) = Http.post $"{url}/{ns}" body
        match status with
        | 200 -> return Ok item
        | 201 -> return Ok item
        | _ -> return makeError status responseText
    }
    let loadPagePrev (<paginationKey> : <paginationKeyType>) (id: string) (limit: int) =
        get $"{url}/{ns}/page/prev?<paginationKey>={<paginationKey>}&id={id}&limit={limit}" (Decode.list Decode.<typename>)
    let loadPageNext (<paginationKey> : <paginationKeyType>) (id: string) (limit: int) =
        get $"{url}/{ns}/page/next?<paginationKey>={<paginationKey>}&id={id}&limit={limit}" (Decode.list Decode.<typename>)
```

Only include `loadAll` if other types will reference this type as a foreign key (like Providers and Places are referenced by Products).

## Step 3 — Register in src/app.fsproj

Call replace_string_in_file on `src/app.fsproj` to insert `Pg$inputs.fs` after the last `Pg*.fs` entry but before `App.fs`.

Read the current `<ItemGroup>` compile list first to find the exact last `Pg*.fs` line, then:
- oldString: that last `Pg*.fs` line, e.g. `    <Compile Include="PgTest.fs" />`
- newString: same line followed by `    <Compile Include="Pg$inputs.fs" />`

**This edit is mandatory — F# requires files in compilation order.**

## Step 4 — Create src/Pg$inputs.fs

Create a new file `src/Pg$inputs.fs` (plural). Base it on `PgProducts.fs` (has foreign-key SelectFields) or `PgPlaces.fs` (simpler, only string/int fields).

```fsharp
module Pg<TypeNames>

open Feliz
open Lib
open WgEdit
open WgList
open Hooks

let use<TypeName>Editor (item: <TypeName>) =
    // One React.useState per mutable field (not ID):
    let <field1>, set<Field1> = React.useState item.<Field1>
    // ...

    // Sync state when item prop changes:
    React.useEffect((fun () ->
        set<Field1> item.<Field1>
        // ...
    ), [| box item |])

    // Build fields list — choose the right WgEdit field type:
    // StrField   — for string fields
    // IntField   — for int fields
    // SelectField — for EnergyKind, PriceType, or foreign-key fields
    //   foreign-key: Value = the ID stored, Offer = use<RefType>().Data from Hooks context
    let fields = [
        StrField { Name = "<fieldName>"; Value = <field>; HandleChange = set<Field> }
        IntField { Name = "<fieldName>"; Value = <field>; HandleChange = set<Field> }
        SelectField { Name = "<fieldName>"; Value = Constants.<KindToText>[<enumField>]; Offer = Constants.<KindSelection>; HandleChange = fun x -> set<EnumField> Constants.<TextToKind>[x] }
        SelectField { Name = "<fk_id>"; Value = <fk_id>; Offer = use<RefType>().Data; HandleChange = set<Fk_id> }
    ]

    let getUpdated<TypeName> () =
        { item with
            <Field1> = <field1>
            // ... }

    fields, getUpdated<TypeName>


[<ReactComponent>]
let Pg<TypeNames>() =

    // For each foreign key displayed in the list, load its map:
    // let <refTypes>Map = use<RefType>().Data |> Map.ofList

    let ctx = use<TypeNames>()   // only if this type is in Hooks context

    let saveAndRefresh item = async {
        let! result = Api.<TypeNames>.saveItem item
        ctx.Refresh()            // only if in Hooks context
        return result
    }

    let fetchBefore (item: <TypeName> option) count =
        let <paginationKey>, id =
            match item with
            | Some x -> x.<PaginationField>, x.ID
            | None -> <emptyDefault>, ""
        Api.<TypeNames>.loadPagePrev <paginationKey> id count

    let fetchAfter (item: <TypeName> option) count =
        let <paginationKey>, id =
            match item with
            | Some x -> x.<PaginationField>, x.ID
            | None -> <emptyDefault>, ""
        Api.<TypeNames>.loadPageNext <paginationKey> id count

    let structure = {
        Headers = [
            { Label = "<fieldName>"; FlexBasis = <width>; DataGetter = fun (x: <TypeName>) -> <displayValue> }
            // For foreign key: lookup from map
            // { Label = "<fk>"; FlexBasis = <width>; DataGetter = fun (x: <TypeName>) ->
            //     match Map.tryFind x.<Fk_ID> <refTypesMap> with
            //     | Some name -> name
            //     | None -> "Unknown" }
        ]
        IdGetter = fun (x: <TypeName>) -> x.ID
    }

    let props = {|
        Structure = structure
        useEditor = fun item -> use<TypeName>Editor item
        ItemNew = Utils.new<TypeName>
        ItemSave = saveAndRefresh
        FetchBefore = fetchBefore
        FetchAfter = fetchAfter
    |}

    WgAgenda.WgAgenda props
```

## Step 5 — Update App.fs

**You must call replace_string_in_file four times on src/App.fs. Do not print code — write every change directly to disk.**

### 4a. Add open after the last `open Pg*` line

Call replace_string_in_file with:
- oldString: the last `open Pg*` line as it appears in the file, e.g. `open PgTest`
- newString: same line followed by a new line `open Pg$inputs`

### 4b. Add Page DU case after the last `| ...` case inside `type Page =`

Call replace_string_in_file with:
- oldString: the last case in `type Page =` as it appears in the file, e.g. `    | Test`
- newString: same line followed by a new line `    | $inputs`

### 4c. Add menu item — insert before the closing `]` of the menus list

Call replace_string_in_file with:
- oldString: the last `renderMenuNavItem` line including the closing `]`, as it appears in the file, e.g. `        renderMenuNavItem "Test" (fun _ -> setCurrentPage Page.Test)]`
- newString: that line split so the last item loses `]`, a new menu item line is added, and `]` closes, e.g.:
  ```
          renderMenuNavItem "Test" (fun _ -> setCurrentPage Page.Test)
          renderMenuNavItem "$inputs" (fun _ -> setCurrentPage Page.$inputs)]
  ```

### 4d. Add page switch case after the last `| Page.*` arm

Call replace_string_in_file with:
- oldString: the last match arm as it appears in the file, e.g. `        | Page.Test -> PgTest ()`
- newString: same line followed by a new line `        | Page.$inputs -> Pg$inputs ()`

**All four replace_string_in_file calls are mandatory. Verify src/App.fs is saved with all changes before moving to the checklist.**

## Checklist

Before finishing, verify:
- [ ] `Utils.new<TypeName>` added with sensible defaults
- [ ] `Encode.<typename>` encodes all fields
- [ ] `Decode.<typename>` decodes all fields
- [ ] `Api.<TypeNames>` module has `saveItem`, `loadPagePrev`, `loadPageNext` (and `loadAll` if needed)
- [ ] `Pg$inputs.fs` registered in `app.fsproj` after the last `Pg*.fs` entry
- [ ] `Pg$inputs.fs` created with module `Pg$inputs`, editor hook `use$inputEditor`, and ReactComponent `Pg$inputs()`
- [ ] `App.fs` has all 4 additions: open, Page case, menu item, switch case
- [ ] No compilation errors (check all field names match the type definition exactly)
