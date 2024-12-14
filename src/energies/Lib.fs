namespace Energies

open System
open Feliz
open Elmish
open Fable.SimpleHttp
open Browser.Dom
open Thoth.Json
open Fable.DateFunctions
open Fable.Core.JsInterop
open Fable.Core

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


module Constants = 
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
        EnergyKind.ElektricityVT, "VT"
        EnergyKind.ElektricityNT, "NT"
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


module Utils =

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


module Encode =

  let energy (ene : Energy) =
    Encode.object [
      "ID", (Encode.string ene.ID)
      "Kind", (Encode.int ( Constants.EnergyKindToInt.[ene.Kind]))
      "Amount", (Encode.int ene.Amount)
      "Info", (Encode.string ene.Info)
      "Created", (Encode.int64 ene.Created)
    ]


module Decode =

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


module Api =
  [<Emit("import.meta.env")>]
  let env: obj = jsNative
  let apiUrl: string = unbox (env?VITE_API_URL)
  let debug: bool =
    match unbox (env?VITE_DEBUG) with
    | "true" -> true
    | _ -> false
  let url = if debug then apiUrl else ( sprintf "%s//%s" window.location.protocol window.location.host )
  printf "debug flag %b" debug
  printf "api url %s" url

  let loadItems =
    async {
      let! (status, responseText) = Http.get $"{url}/energies"
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
      let! (status, responseText) = Http.get $"{url}/lastenergies?count=10"
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

  let loadPrevRows (mark : int64 * string) (limit: int) =
    async {
      let pin, id = mark
      let! (status, responseText) = Http.get $"{url}/energies?prev={limit}&pin={pin}&id={id}"
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

  let loadNextRows (mark : int64 * string) (limit: int) =
    async {
      let pin, id = mark
      let! (status, responseText) = Http.get $"{url}/energies?next={limit}&pin={pin}&id={id}"
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
      let! (status, responseText) = Http.post $"{url}/energies" body
      match status with
      | 200 -> return Ok item
      | 201 -> return Ok item
      | _ -> return Error responseText
    }


module Render =

  type Inputs = { 
    date: ReactElement
    time: ReactElement
    kind: ReactElement
    amount: ReactElement
    unit: ReactElement
    info: ReactElement
  }

  let gridRow (item : Energy) =
    let kind = Constants.EnergyKindToText.[item.Kind]
    let created = (Utils.unixTimeToLocalDateTime item.Created).ToString("dd.MM.yyyy HH:mm")
    let amount = $"{item.Amount} {Constants.EnergyKindToUnit.[item.Kind]}"
    Html.div [
      prop.classes [ "fg-row" ]
      prop.children [
        // Html.div [ prop.classes [ "fg-cell" ]; prop.children [ Html.text item.ID ] ]
        Html.div [ prop.classes [ "fg-scell" ];  prop.style [ style.custom  ("--flex-basis", "10%" ) ]; prop.children [ Html.text kind ] ]
        Html.div [ prop.classes [ "fg-scell" ]; prop.style [ style.custom  ("--flex-basis", "30%" ) ]; prop.children [ Html.text created ] ]
        Html.div [ prop.classes [ "fg-scell" ];prop.style [ style.custom  ("--flex-basis", "15%" ) ];  prop.children [ Html.text amount ] ]
        Html.div [ prop.classes [ "fg-ncell" ]; prop.children [ Html.text item.Info] ]
      ]
    ]

  let gridRowEmpty () =
    let invisible = Html.div [ prop.style [ style.visibility.hidden ]; prop.children [Html.text " x "]  ]
    Html.div [
      prop.classes [ "fg-row" ]
      prop.children [
        Html.div [ prop.classes [ "fg-scell" ]; prop.style [ style.custom  ("--flex-basis", "10%" ) ]; prop.children [ invisible ] ]
        Html.div [ prop.classes [ "fg-scell" ]; prop.style [ style.custom  ("--flex-basis", "30%" ) ]; prop.children [ invisible ] ]
        Html.div [ prop.classes [ "fg-scell" ];prop.style [ style.custom  ("--flex-basis", "15%" ) ];  prop.children [ invisible ] ]
        Html.div [ prop.classes [ "fg-ncell" ]; prop.children [ invisible ] ]
      ]
    ]

  let grid (renderRows : unit -> ReactElement list) =
    Html.div [
      prop.classes [ "fg-grid" ]
      prop.children (renderRows ())
    ]

  let edit (inputs: Inputs) =
    Html.div [ 
      prop.classes [ "edit-box" ]
      prop.children [
        inputs.kind; inputs.date; inputs.time; inputs.amount ; inputs.unit ; inputs.info
      ]
    ]
