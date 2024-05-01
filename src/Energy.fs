namespace Energies

open System
open Feliz
open Elmish

type EnergyDbType =
  { ID : String
    Amount : int64
    Info : string
    Created : int64 }

type EnergyEditType =
  { ID : String
    Amount : EditUtils.Field
    Info : EditUtils.Field
    Created : EditUtils.Field }


module Utils =
  let amountVD = editIsValid "^\\d+$"

  let dateEditFormat = "dd.MM.yyyy HH:mm:ss"

  let unixTimeToString (format : string) (unixTimeSeconds : int64) : string =
    let dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds (unixTimeSeconds)
    dateTimeOffset.ToString (format)

  // let stringToUnixTime (format : string) (dateTimeString : string) : int64 option =
  //   match DateTimeOffset.TryParseExact (dateTimeString, format, null, System.Globalization.DateTimeStyles.None) with
  //   | true, dateTimeOffset -> Some (dateTimeOffset.ToUnixTimeSeconds ())
  //   | false, _ -> None

  let createEdit amount info date =
    { ID = Guid.NewGuid().ToString ()
      Amount =
        { Input = amount.ToString ()
          Valid = true }
      Info = { Input = info ; Valid = true }
      Created =
        { Input = date.ToString ()
          Valid = true } }

  let createEditFromDB (energyDB : EnergyDbType) =
    { ID = energyDB.ID
      Amount =
        { Input = energyDB.Amount.ToString ()
          Valid = true }
      Info = { Input = energyDB.Info ; Valid = true }
      Created =
        { Input = unixTimeToString dateEditFormat energyDB.Created
          Valid = true } }


module EditEnergy =

  type State = { Energy : EnergyEditType }

  type Msg =
    | SetAmount of string
    | SetInfo of string
    | SetDate of string

  let empty () =
    { Energy = Utils.createEdit 0 "" DateTime.Now }

  let amountVD = editIsValid "^\\d+$"

  let update msg state =
    match msg with
    | SetAmount x ->
      let state = { state with Energy.Amount = { Input = x ; Valid = amountVD (x) } }
      state, Cmd.none
    | SetInfo x ->
      let state = { state with Energy.Info = { Input = x ; Valid = true } }
      state, Cmd.none
    | SetDate x ->
      let state = { state with Energy.Created = { Input = x ; Valid = true } }
      state, Cmd.none

  let renderField label (field : EditUtils.Field) (onchange : string -> unit) =
    Html.div [
      prop.classes [ "field" ]
      prop.children [
        Html.label [
          prop.text (label : string)
        ]
        Html.div [
          prop.classes [ "control" ]
          prop.children [
            Html.input [
              prop.classes [
                if field.Valid then
                  "input"
                else
                  "input is-danger"
              ]
              prop.type' "text"
              prop.placeholder "Amount"
              prop.valueOrDefault (field.Input)
              prop.onChange (onchange)
            ]
          ]
        ]
      ]
    ]

  let render (state : State) (dispatch : Msg -> unit) =
    Html.div [
      renderField "Date" state.Energy.Created (SetDate >> dispatch)
      renderField "Amount" state.Energy.Amount (SetAmount >> dispatch)
      renderField "Info" state.Energy.Info (SetInfo >> dispatch)
    ]


module ListEnergy =

  open Fable.SimpleHttp
  open Thoth.Json


  type State =
    { Items : Deferred<List<EnergyDbType>> }

  type Msg = | LoadItems of AsyncOperationEvent<Result<List<EnergyDbType>, string>>

  let empty () = { Items = HasNotStartedYet }

  let decoder : Decoder<EnergyDbType> =
    Decode.object (fun fields ->
      { ID = fields.Required.At [ "ID" ] Decode.string
        Amount = fields.Required.At [ "Amount" ] Decode.int64
        Info = fields.Required.At [ "Info" ] Decode.string
        Created = fields.Required.At [ "Created" ] Decode.int64 }
    )

  let loadItems =
    async {
      let! (status, responseText) = Http.get "http://localhost:8085/energies"

      match status with
      | 200 ->
        let items = Decode.fromString (Decode.list decoder) responseText

        match items with
        | Ok x -> return Msg.LoadItems (Finished (Ok (x)))
        | Error parseError -> return Msg.LoadItems (Finished (Error parseError))
      | _ ->
        // non-OK response goes finishes with an error
        return Msg.LoadItems (Finished (Error responseText))
    }


  let update msg state =
    match msg with
    | Msg.LoadItems Started ->
      let state = { state with Items = InProgress }
      state, Cmd.fromAsync (loadItems)
    | Msg.LoadItems (Finished (Ok items)) ->
      let state = { state with Items = Resolved (items) }
      state, Cmd.none
    | Msg.LoadItems (Finished (Error text)) ->
      Console.WriteLine text
      let state = { state with Items = Resolved ([]) }
      state, Cmd.none

  let renderField (field : EditUtils.Field) =
    Html.div [
      prop.classes [
        if field.Valid then
          "cell"
        else
          "cell is-danger"
      ]
      prop.children [ Html.span field.Input ]
    ]

  let renderCell (text : string) =
    Html.div [
      prop.classes [ "cell" ]
      prop.children [ Html.span text ]
    ]

  let renderItem (item : EnergyEditType) =
    [ renderCell item.ID
      renderField item.Created
      renderField item.Info
      renderField item.Amount ]

  let render (state : State) (dispatch : Msg -> unit) =
    match state.Items with
    | HasNotStartedYet -> Html.div "has not started"
    | InProgress -> Html.div "in progress"
    | Resolved items ->
      let bbb = List.collect (Utils.createEditFromDB >> renderItem) items

      Html.div [
        prop.classes [
          "fixed-grid"
          "has-1-cols-mobile"
          "has-4-cols-tablet"
        ]
        prop.children [
          Html.div [
            prop.classes [ "grid" ]
            prop.children (List.collect (Utils.createEditFromDB >> renderItem) items)
          ]
        ]
      ]


module Energy =

  type State =
    { Edit : EditEnergy.State
      InEdit : bool
      Rows : ListEnergy.State }

  type Msg =
    | AddNew
    | LoadRows
    | EditMsg of EditEnergy.Msg
    | ListMsg of ListEnergy.Msg

  let init () : State =
    { Edit = EditEnergy.empty ()
      InEdit = false
      Rows = ListEnergy.empty () }

  let update msg state =
    match msg with
    | AddNew ->
      let state =
        { state with
            Edit = EditEnergy.empty ()
            InEdit = true }

      state, Cmd.none
    | LoadRows -> state, Cmd.ofMsg (ListMsg (ListEnergy.Msg.LoadItems (Started)))
    | EditMsg msg ->
      let newState, editCmd = EditEnergy.update msg state.Edit
      { state with Edit = newState }, Cmd.map EditMsg editCmd
    | ListMsg msg ->
      let newstate, cmd = ListEnergy.update msg state.Rows
      { state with Rows = newstate }, Cmd.map ListMsg cmd

  let render (state : State) (dispatch : Msg -> unit) =
    Html.div [

      match state.InEdit with
      | true -> EditEnergy.render state.Edit (fun msg -> dispatch (EditMsg msg))
      | _ ->
        Html.div [
          Html.div [
            Html.button [
              prop.text "Add new energy"
              prop.onClick (fun _ -> dispatch AddNew)
            ]
          ]
        ]

      Html.div [
        Html.div [
          Html.button [
            prop.text "Load rows"
            prop.onClick (fun _ -> dispatch LoadRows)
          ]
        ]
      ]

      ListEnergy.render state.Rows (fun msg -> dispatch (ListMsg msg))
    ]
