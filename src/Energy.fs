namespace Energies

open System
open Feliz
open Elmish
open Fable.SimpleHttp
open Thoth.Json
open Fable.DateFunctions

type EnergyKind =
  | ElektricityVT
  | ElektricityNT
  | Gas
  | Water

type Energy =
  { ID : string
    Kind: EnergyKind
    Amount : int
    Info : string
    Created : int64 }


module private Constants = 
  [<Literal>]
  let KWH = "kWh"
  [<Literal>]
  let M3 = "m3"

  let EnergyKindToUnit = 
    Map [
        EnergyKind.ElektricityVT, KWH
        EnergyKind.ElektricityNT, KWH
        EnergyKind.Gas, M3
        EnergyKind.Water, M3
    ]

  let EnergyKindToText = 
    Map [
        EnergyKind.ElektricityVT, "Electricity VT"
        EnergyKind.ElektricityNT, "Electricity NT"
        EnergyKind.Gas, "Gas"
        EnergyKind.Water, "Water"
    ]

  let EnergyKindToInt = 
    Map [
        EnergyKind.ElektricityVT, 1
        EnergyKind.ElektricityNT, 2
        EnergyKind.Gas, 3
        EnergyKind.Water, 4
    ]

  let IntToEnergyKind = 
    Map [
        1, EnergyKind.ElektricityVT
        2, EnergyKind.ElektricityNT
        3, EnergyKind.Gas
        4, EnergyKind.Water
    ]


module private Utils =

  let unixTimeToLocalDateTime (unixTimeSeconds : int64) : DateTime =
    let dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds (unixTimeSeconds)
    dateTimeOffset.ToLocalTime().LocalDateTime

  let localDateTimeToUnixTime (datetime : DateTime) : int64 =
    let unixEpoch = DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    let timeSpan = datetime.ToUniversalTime () - unixEpoch
    Convert.ToInt64 (timeSpan.TotalSeconds)

  let toUnixTimeSeconds (dateTime : DateTime) : int64 =
    let unixEpoch = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Unix epoch in UTC
    let timeSpan = dateTime - unixEpoch
    timeSpan.Ticks / (int64) 10000000

  let jsTimestampToDateTime (timestamp: float) : DateTime =
    let dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(int64 timestamp)
    dateTimeOffset.DateTime

  let joinDateAndTime (date: DateTime) (time: DateTime) =
    let dateo = DateOnly(date.Year, date.Month, date.Day)
    let timeo = TimeOnly (time.Hour, time.Minute, time.Second)
    let dateTime = DateTime(dateo.Year, dateo.Month, dateo.Day, timeo.Hour, timeo.Minute, timeo.Second)
    dateTime

  let intToEnergyKind (value: int): EnergyKind option =
    match value with
    | x when (Map.containsKey value Constants.IntToEnergyKind) -> Some(Constants.IntToEnergyKind.[x])
    | _ -> None


module private Encode =

  let energy (ene : Energy) =
    Encode.object [
      "ID", (Encode.string ene.ID)
      "Kind", (Encode.int ( Constants.EnergyKindToInt.[ene.Kind]))
      "Amount", (Encode.int ene.Amount)
      "Info", (Encode.string ene.Info)
      "Created", (Encode.int64 ene.Created)
    ]


module private Decode =

  let energyKind: Decoder<EnergyKind> =
    fun path value ->
      if Decode.Helpers.isNumber value then
        let value : int = unbox value
        match Utils.intToEnergyKind(value) with
        | Some x -> Ok x
        | None -> (path, BadPrimitive("int value mapping kind out of range", value)) |> Error
      else
        (path, BadPrimitive("value mapping kind is not a number", value)) |> Error

  let energy : Decoder<Energy> =
    Decode.object (fun fields ->
      { ID = fields.Required.At [ "ID" ] Decode.string
        Amount = fields.Required.At [ "Amount" ] Decode.int
        Info = fields.Required.At [ "Info" ] Decode.string
        Created = fields.Required.At [ "Created" ] Decode.int64
        Kind = fields.Required.At [ "Kind" ] energyKind
      }
    )


module private Api =
  let loadItems =
    async {
      let! (status, responseText) = Http.get "http://localhost:8085/energies"
      match status with
      | 200 ->
        let items = Decode.fromString (Decode.list Decode.energy) responseText
        match items with
        | Ok x -> return Ok (x)
        | Error parseError -> return Error parseError
      | _ ->
        // non-OK response goes finishes with an error
        return Error responseText
    }

  let loadLastRows =
    async {
      let! (status, responseText) = Http.get "http://localhost:8085/lastenergies?count=10"
      match status with
      | 200 ->
        let items = Decode.fromString (Decode.list Decode.energy) responseText
        match items with
        | Ok x -> return Ok (x)
        | Error parseError -> return Error parseError
      | _ ->
        // non-OK response goes finishes with an error
        return Error responseText
    }

  let saveItem (item : Energy) =
    async {
      let json = Encode.energy item
      let body = Encode.toString 2 json
      let! (status, responseText) = Http.post "http://localhost:8085/energies" body
      match status with
      | 200 -> return Ok item
      | 201 -> return Ok item
      | _ -> return Error responseText
    }


module private Render =

  type Inputs = { 
    date: ReactElement
    time: ReactElement
    kind: ReactElement
    amount: ReactElement
    info: ReactElement
  }

  let gridRow (item : Energy) =
    let kind = Constants.EnergyKindToText.[item.Kind]
    let created = (Utils.unixTimeToLocalDateTime item.Created).ToString("dd.MM.yyyy HH:mm")
    let amount = $"{item.Amount} {Constants.EnergyKindToUnit.[item.Kind]}"
    [
      // Html.div [ prop.classes [ "cell" ]; prop.children [ Html.text item.ID ] ]
      Html.div [ prop.classes [ "cell" ]; prop.children [ Html.text kind ] ]
      Html.div [ prop.classes [ "cell" ]; prop.children [ Html.text created ] ]
      Html.div [ prop.classes [ "cell" ]; prop.children [ Html.text amount ] ]
      Html.div [ prop.classes [ "cell" ]; prop.children [ Html.text item.Info] ]
    ]

  let grid (renderRows : unit -> ReactElement list) =
    Html.div [
      prop.classes [ "columns" ]
      prop.children [
        Html.div [
          prop.classes [ "column" ]
          prop.children [
            Html.div [
              prop.classes ["fixed-grid"; "has-1-cols-mobile"; "has-2-cols-tablet"; "has-4-cols-desktop"]
              prop.children [
                Html.div [
                  prop.classes ["grid is-gap-0"]
                  prop.children (renderRows ())
                ]
              ]
            ]
          ]
        ]
      ]
    ]

  let edit (inputs: Inputs) =
    Html.div [ 
      prop.classes [ "column" ]
      prop.children [

        Html.div [ 
          prop.classes [ "column" ]
          prop.style [ style.backgroundColor "red" ]
          prop.children [
            Html.div [ 
              prop.classes [ "field"; "has-addons" ]
              prop.children [
                Html.div [
                  prop.classes [ "control" ]
                  prop.children [
                    Html.div [
                      prop.classes [ "select" ]
                      prop.children [ inputs.kind ]
                    ]
                  ]
                ]
                Html.div [
                  prop.classes [ "control" ]
                  prop.children [ inputs.date ]
                ]
                Html.div [
                  prop.classes [ "control" ]
                  prop.children [ inputs.time ]
                ]
              ]
            ]
          ]
        ]

        Html.div [
          prop.classes [ "column" ; "is-one-fifth" ]
          prop.style [ style.backgroundColor "blue" ]
          prop.children [
            Html.div [
              prop.classes [ "field has-addons" ]
              prop.children [
                Html.div [
                  prop.classes [ "control is-expanded" ]
                  prop.children [ inputs.amount ]
                ]
                Html.div [
                  prop.classes [ "control" ]
                  prop.children [
                    Html.a [ 
                      prop.classes [ "button is-static" ]
                      prop.text "kWh"
                    ]
                  ]
                ]
              ]
            ]
          ]
        ]

        Html.div [
          prop.classes [ "column" ]
          prop.style [ style.backgroundColor "lime" ]
          prop.children [
            Html.div [ 
              prop.classes [ "field" ]
              prop.children [
                Html.div [
                  prop.classes [ "control"; "is-expanded" ]
                  prop.children [ inputs.info ]
                ]
              ]
            ]
          ]
        ]

      ]
    ]


module WgEdit =

  type State = { 
    ID : string
    Kind: EnergyKind
    Amount : int
    Info : string
    Created : DateTime 
  }

  type Msg =
  | Date of float
  | Time of float
  | Kind of string
  | Amount of string
  | Info of string

  let stateToEnergy (state: State): Energy = 
    {
      ID = state.ID
      Kind = state.Kind
      Amount = state.Amount
      Info = state.Info
      Created = (Utils.localDateTimeToUnixTime state.Created)
    }


  let empty () =
    {   ID = Guid.NewGuid().ToString()
        Kind = EnergyKind.Gas
        Amount = 0
        Info = ""
        Created = DateTime.Now
    }

  let update msg state =
    match msg with
    | Msg.Date x ->
      let created = Utils.joinDateAndTime (Utils.jsTimestampToDateTime x) state.Created
      let state = { state with Created = created }
      state, Cmd.none
    | Msg.Time x ->
      let created = Utils.joinDateAndTime state.Created (Utils.jsTimestampToDateTime x)
      let state = { state with Created = created }
      state, Cmd.none
    | Msg.Kind x ->
      match System.Int32.TryParse(x) with
      | (true, intValue) ->
        let state = 
          match Utils.intToEnergyKind(intValue) with
          | Some x -> { state with Kind = x }
          | None -> state
        state, Cmd.none
      | (false, _) -> state, Cmd.none
    | Msg.Amount x ->
      let state = 
        match System.Int32.TryParse x with
        | (true, amount) -> { state with Amount = amount }
        | (false, _) -> state
      state, Cmd.none
    | Msg.Info x ->
      let state = { state with Info = x }
      state, Cmd.none

  let private renderInputs (state : State) (dispatch : Msg -> unit) : Render.Inputs =

    let kindSelectOptions = 
      Constants.EnergyKindToText
      |> Map.toList
      |> List.map ( fun (energyKind, text) -> 
          Html.option [ prop.text text; prop.value Constants.EnergyKindToInt.[energyKind] ] )

    { 
      date = Html.input [
        prop.classes [ "input" ]
        prop.type' "date"
        prop.value (state.Created.ToString("yyyy-MM-dd"))
        prop.onChange (Msg.Date >> dispatch)
      ]
      time = Html.input [
        prop.classes [ "input" ]
        prop.type' "time"
        prop.value (state.Created.ToString("HH:mm"))
        prop.onChange (Msg.Time >> dispatch)
      ]
      kind = Html.select [
          prop.children kindSelectOptions
          prop.onChange (Msg.Kind >> dispatch)
      ]
      amount = Html.input [
        prop.classes [ "input" ]
        prop.type' "text" 
        prop.placeholder "amount"
        prop.value (state.Amount.ToString())
        prop.onChange (Msg.Amount >> dispatch)
      ]
      info = Html.input [ 
        prop.classes [ "input" ] 
        prop.type' "text"
        prop.placeholder "remark"
        prop.value (state.Info)
        prop.onChange (Msg.Info >> dispatch)
      ]
    }

  let render (state : State) (dispatch : Msg -> unit) =
    let inputs = renderInputs state dispatch
    Render.edit inputs


module Energy =

  type State =
    { 
      LastRows : Deferred<List<Energy>>
      WgEdit: WgEdit.State
      LastEdits: List<Energy>
    }

  type Msg =
  | InitPage
  | LoadLastRows of AsyncOperationEvent<Result<List<Energy>, string>>
  | WgEdit of WgEdit.Msg
  | SaveItem of AsyncOperationEvent<Result<Energy, string>>

  let init () : State =
    { 
      LastRows = HasNotStartedYet
      WgEdit = WgEdit.empty()
      LastEdits = []
    }

  let update msg state =
    match msg with
    | WgEdit x ->
      let newstate, newcmd = WgEdit.update x state.WgEdit
      { state with WgEdit = newstate}, newcmd
    | Msg.InitPage ->
      state, Cmd.ofMsg (Msg.LoadLastRows StartIt)
    | Msg.LoadLastRows x ->
      match x with
      | StartIt ->
        let state = { state with LastRows = InProgress }
        state, Cmd.fromAsync (Async.map (fun x -> Msg.LoadLastRows(FinishIt(x))) Api.loadLastRows)
      | FinishIt (Ok items) ->
        let state = { state with LastRows = Resolved (items); LastEdits=items }
        state, Cmd.none
      | FinishIt (Error text) ->
        let state = { state with LastRows = Resolved ([]) }
        state, Cmd.none
    | Msg.SaveItem x ->
      match x with
      | StartIt ->
        let asyncSave = (Api.saveItem(WgEdit.stateToEnergy state.WgEdit))
        state, Cmd.fromAsync (Async.map (fun x -> Msg.SaveItem(FinishIt(x))) asyncSave )
      | FinishIt (Ok item) ->
        let lastedits = state.LastEdits @ [item]
        let lastedits2 = 
          if List.length(lastedits) > 10 then
            List.skip (List.length lastedits - 10) lastedits
          else
            lastedits
        {state with LastEdits = lastedits2}, Cmd.none
      | FinishIt (Error text) ->
        state, Cmd.none

  let render (state : State) (dispatch : Msg -> unit) =
    let grid =
      Render.grid (fun () -> List.collect Render.gridRow state.LastEdits)
    let edit = WgEdit.render state.WgEdit ( fun x -> dispatch (WgEdit x) )

    let addButton =
      Html.div [
        prop.classes [ "columns" ]
        prop.children [
          Html.div [
            prop.classes [ "column" ]
            prop.children [
              Html.button [
                prop.classes [ "button"; "is-primary"; "is-pulled-right" ]
                prop.text "Add"
                prop.onClick ( fun _ -> dispatch (SaveItem StartIt) )
              ]
            ]
          ]
        ]
      ]

    [ grid; edit; addButton]
