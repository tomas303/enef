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

type EnergyDbType =
  { ID : String
    Kind: EnergyKind
    Amount : int64
    Info : string
    Created : int64 }

type EnergyEditType =
  { ID : String
    Amount : EditUtils.Field<string>
    Info : EditUtils.Field<string>
    Created : EditUtils.Field<DateTime> }


module Constants = 
  [<Literal>]
  let KWH = "kWh"
  [<Literal>]
  let M3 = "m3"

  let EnergyKindToUnit = 
    Map [
        ElektricityVT, KWH
        ElektricityNT, KWH
        Gas, M3
        Water, M3
    ]


module Encode =
  let Energy (ene : EnergyDbType) =
    Encode.object [
      "ID", (Encode.string ene.ID)
      "Amount", (Encode.int64 ene.Amount)
      "Info", (Encode.string ene.Info)
      "Created", (Encode.int64 ene.Created)
    ]


module Decode =

  let EnergyKind: Decoder<EnergyKind> =
    fun path value ->
      if Decode.Helpers.isNumber value then
        let value : int = unbox value
        match value with
          | 0 -> Ok EnergyKind.ElektricityNT
          | 1 -> Ok EnergyKind.ElektricityVT
          | 2 -> Ok EnergyKind.Gas
          | 3 -> Ok EnergyKind.Water
          | _ -> (path, BadPrimitive("int value mapping kind out of range", value)) |> Error
      else
        (path, BadPrimitive("value mapping kind is not a number", value)) |> Error

  let Energy : Decoder<EnergyDbType> =
    Decode.object (fun fields ->
      { ID = fields.Required.At [ "ID" ] Decode.string
        Amount = fields.Required.At [ "Amount" ] Decode.int64
        Info = fields.Required.At [ "Info" ] Decode.string
        Created = fields.Required.At [ "Created" ] Decode.int64
        Kind = fields.Required.At [ "Kind" ] EnergyKind
      }
    )


module Api =
  let loadItems =
    async {
      let! (status, responseText) = Http.get "http://localhost:8085/energies"

      match status with
      | 200 ->
        let items = Decode.fromString (Decode.list Decode.Energy) responseText

        match items with
        | Ok x -> return Ok (x)
        | Error parseError -> return Error parseError
      | _ ->
        // non-OK response goes finishes with an error
        return Error responseText
    }


module Render =

  let GridRow (item : EnergyDbType) =
    let created = item.Created.ToString("dd.MM.yyyy HH:mm")
    let amount = $"{item.Amount} {Constants.EnergyKindToUnit.[item.Kind]}"
    [
      Html.div [ prop.classes [ "cell" ]; prop.children [ Html.text item.ID ] ]
      Html.div [ prop.classes [ "cell" ]; prop.children [ Html.text created ] ]
      Html.div [ prop.classes [ "cell" ]; prop.children [ Html.text amount ] ]
      Html.div [ prop.classes [ "cell" ]; prop.children [ Html.text item.Info] ]
    ]

  let Grid (renderRows : unit -> ReactElement list) =
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


module Utils =

  let amountVD = editIsValid "^\\d+$"

  // let dateEditFormat = "dd.MM.yyyy HH:mm:ss"
  let dateEditFormat = "dd.MM HH:mm"

  let unixTimeToString (format : string) (unixTimeSeconds : int64) : string =
    let dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds (unixTimeSeconds)
    dateTimeOffset.ToString (format)

  // let stringToUnixTime (format : string) (dateTimeString : string) : int64 option =
  //   match DateTimeOffset.TryParseExact (dateTimeString, format, null, System.Globalization.DateTimeStyles.None) with
  //   | true, dateTimeOffset -> Some (dateTimeOffset.ToUnixTimeSeconds ())
  //   | false, _ -> None

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


  let stringToUnixTime (format : string) (dateTimeString : string) : int64 =
    let dt =
      // DateTime.ParseExact (dateTimeString, format, null, System.Globalization.DateTimeStyles.None)
      DateTime.Parse (dateTimeString)

    toUnixTimeSeconds (dt)



  // let createEdit amount info date =
  //   { ID = Guid.NewGuid().ToString ()
  //     Amount =
  //       { Input = amount.ToString ()
  //         Valid = true }
  //     Info = { Input = info ; Valid = true }
  //     Created = { Input = date ; Valid = true } }

  // let createEditFromDB (energyDB : EnergyDbType) =
  //   { ID = energyDB.ID
  //     Amount =
  //       { Input = energyDB.Amount.ToString ()
  //         Valid = true }
  //     Info = { Input = energyDB.Info ; Valid = true }
  //     Created =
  //       { Input = unixTimeToLocalDateTime energyDB.Created
  //         Valid = true } }

  // let createDBFromEdit (ene : EnergyEditType) : EnergyDbType =
  //   { ID = ene.ID
  //     Amount =
  //       (if ene.Amount.Valid then
  //          Int64.Parse (ene.Amount.Input)
  //        else
  //          0)
  //     Info =
  //       (if ene.Info.Valid then
  //          ene.Info.Input
  //        else
  //          "")
  //     Created =
  //       (if ene.Created.Valid then
  //          localDateTimeToUnixTime (ene.Created.Input)
  //        else
  //          0) }


// module EditEnergy =

//   type State = { Energy : EnergyEditType }

//   type Msg =
//     | SetAmount of string
//     | SetInfo of string
//     | SetDate of DateTime

//   let empty () =
//     { Energy = Utils.createEdit 11 "new" DateTime.Now }

//   let amountVD = editIsValid "^\\d+$"

//   let update msg state =
//     match msg with
//     | SetAmount x ->
//       let state = { state with Energy.Amount = { Input = x ; Valid = amountVD (x) } }
//       state, Cmd.none
//     | SetInfo x ->
//       let state = { state with Energy.Info = { Input = x ; Valid = true } }
//       state, Cmd.none
//     | SetDate x ->
//       let state = { state with Energy.Created = { Input = x ; Valid = true } }
//       state, Cmd.none

//   let render (state : State) (dispatch : Msg -> unit) =
//     Html.div [ 
//       prop.classes [ "column"; "is-3" ]
//       prop.children [ 
//         Html.div [
//           prop.classes [ "field"; "has-addons" ]
//           prop.children [
//             Html.div [
//               prop.classes [ "control" ]
//               prop.children [
//                 Html.input [
//                   prop.classes [ "input" ]
//                   prop.placeholder "date"
//                   prop.type' "date"
//                   //prop.onTextChange (SetEditedDescription >> dispatch)
//                 ]
//               ]
//             ]
//             Html.div [
//               prop.classes [ "control"; "is-expanded" ]
//               prop.children [
//                 Html.input [
//                   prop.classes [ "input" ]
//                   prop.placeholder "text"
//                   prop.type' "text"
//                   //prop.onTextChange (SetEditedDescription >> dispatch)
//                 ]
//               ]
//             ]
//             Html.div [
//               prop.classes [ "control" ]
//               prop.children [
//                 Html.button [
//                   prop.classes [ "button"; "is-info" ]
//                   prop.text "remark"
//                   //prop.onTextChange (SetEditedDescription >> dispatch)
//                 ]
//               ]
//             ]
//           ]
//         ]
//         Html.div [
//           prop.classes [ "field" ]
//           prop.children [
//             Html.div [
//               prop.classes [ "control" ]
//               prop.children [
//                 Html.input [
//                   prop.classes [ "input"]
//                   prop.type' "text"
//                   prop.placeholder "text"
//                   //prop.onTextChange (SetEditedDescription >> dispatch)
//                 ]
//               ]
//             ]
//           ]
//         ]
//       ]
//     ]


module Energy =

  type State =
    { 
      Items : Deferred<List<EnergyDbType>>
    }

  type Msg =
  | LoadItems of AsyncOperationEvent<Result<List<EnergyDbType>, string>>

  let init () : State =
    { 
      Items = HasNotStartedYet
    }

  let update msg state =
    match msg with
    | Msg.LoadItems Started ->
      let state = { state with Items = InProgress }
      state, Cmd.fromAsync (Async.map (fun x -> Msg.LoadItems(Finished(x))) Api.loadItems)
    | Msg.LoadItems (Finished (Ok items)) ->
      let state = { state with Items = Resolved (items) }
      state, Cmd.none
    | Msg.LoadItems (Finished (Error text)) ->
      Console.WriteLine text
      let state = { state with Items = Resolved ([]) }
      state, Cmd.none


  let render (state : State) (dispatch : Msg -> unit) =
    match state.Items with
    | HasNotStartedYet -> Html.div "has not started"
    | InProgress -> Html.div "in progress"
    | Resolved items -> Render.Grid (fun () -> List.collect Render.GridRow items)
