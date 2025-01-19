module Lib

open System
open Feliz
open Elmish
open Fable.SimpleHttp
open Browser.Dom
open Thoth.Json
open Fable.DateFunctions
open Fable.Core.JsInterop
open Fable.Core

type Place = {
    ID : string
    Name: String
    CircuitBreakerCurrent : int
}

type Provider = {
    ID : string
    Name: String
}

type Product = {
    ID : string
    Name: String
    Provider_ID : string
}

type EnergyKind =
    | ElektricityVT
    | ElektricityNT
    | Gas
    | Water

type PriceType =
    | Volume
    | Month
    | Tax

type Energy = {
    ID : string
    Kind: EnergyKind
    Amount : int
    Info : string
    Created : int64 
    Place_ID : string
}

type Price = {
    ID: string
    Value: int
    FromDate: string
    Provider_ID: string
    PriceType: PriceType
    EnergyKind: EnergyKind
}

module Dbg =
    
    let wl (x: string) = System.Console.WriteLine x


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

    let TextToEnergyKind = 
        Map [
            "VT", EnergyKind.ElektricityVT
            "NT", EnergyKind.ElektricityNT
            "Gas", EnergyKind.Gas
            "Water", EnergyKind.Water
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

    let PriceTypeToText = 
        Map [
            PriceType.Volume, "Per volume"
            PriceType.Month, "Monthly"
            PriceType.Tax, "Tax(%)"
        ]

    let TextToPriceType = 
        Map [
            "Per volume", PriceType.Volume
            "Monthly", PriceType.Month
            "Tax(%)", PriceType.Tax
        ]

    let PriceTypeToInt = 
        Map [
            PriceType.Volume, 1
            PriceType.Month, 2
            PriceType.Tax, 3
        ]

    let IntToPriceType = 
        Map [
            1, PriceType.Volume
            2, PriceType.Month
            3, PriceType.Tax
        ]

    let EnergyKindSelection = [for x in TextToEnergyKind.Keys -> (x, x)]
    let PriceTypeSelection = [for x in TextToPriceType.Keys -> (x, x)]

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

    let intToPriceType (value: int): PriceType option =
        match value with
        | x when (Map.containsKey value Constants.IntToPriceType) -> Some(Constants.IntToPriceType.[x])
        | _ -> None

    let newID () = System.Guid.NewGuid().ToString()

    let newEnergy () = {
        ID = newID ()
        Kind = EnergyKind.ElektricityNT
        Amount = 0
        Info = ""
        Created = localDateTimeToUnixTime System.DateTime.Now
        Place_ID = ""
    }

    let newPlace () = {
        ID = newID ()
        Name = ""
        CircuitBreakerCurrent = 0
    }

    let newProvider () = {
        ID = newID ()
        Name = ""
    }

    let newProduct () = {
        ID = newID ()
        Name = ""
        Provider_ID = ""
    }

    let newPrice () = {
        ID = newID ()
        Value = 0
        FromDate = DateTime.Now.ToString("yyyyMMdd")
        Provider_ID = ""
        PriceType = PriceType.Volume
        EnergyKind = EnergyKind.ElektricityNT
    }

module Encode =

    let energy (ene : Energy) =
        Encode.object [
            "ID", (Encode.string ene.ID)
            "Kind", (Encode.int ( Constants.EnergyKindToInt.[ene.Kind]))
            "Amount", (Encode.int ene.Amount)
            "Info", (Encode.string ene.Info)
            "Created", (Encode.int64 ene.Created)
            "Place_ID", (Encode.string ene.Place_ID)
        ]

    let place (pl : Place) =
        Encode.object [
            "ID", (Encode.string pl.ID)
            "Name", (Encode.string pl.Name)
            "CircuitBreakerCurrent", (Encode.int pl.CircuitBreakerCurrent)
        ]

    let provider (pr : Provider) =
        Encode.object [
            "ID", (Encode.string pr.ID)
            "Name", (Encode.string pr.Name)
        ]

    let product (pr : Product) =
        Encode.object [
            "ID", (Encode.string pr.ID)
            "Name", (Encode.string pr.Name)
            "Provider_ID", (Encode.string pr.Provider_ID)
        ]

    let price (pr : Price) =
        Encode.object [
            "ID", (Encode.string pr.ID)
            "Value", (Encode.int pr.Value)
            "FromDate", (Encode.string pr.FromDate)
            "Provider_ID", (Encode.string pr.Provider_ID)
            "PriceType", (Encode.int (Constants.PriceTypeToInt.[pr.PriceType]))
            "EnergyKind", (Encode.int (Constants.EnergyKindToInt.[pr.EnergyKind]))
        ]

module Decode =

    let energyKind: Decoder<EnergyKind> =
        fun path value ->
        if Decode.Helpers.isNumber value then
            let uval : int = unbox value
            match Utils.intToEnergyKind(uval) with
            | Some x -> Ok x
            | None -> (path, BadPrimitive("int value mapping kind out of range", value)) |> Error
        else
            (path, BadPrimitive("value mapping kind is not a number", value)) |> Error

    let priceType: Decoder<PriceType> =
        fun path value ->
        if Decode.Helpers.isNumber value then
            let value : int = unbox value
            match Utils.intToPriceType(value) with
            | Some x -> Ok x
            | None -> (path, BadPrimitive("int value mapping type out of range", value)) |> Error
        else
            (path, BadPrimitive("value mapping kind is not a number", value)) |> Error

    let energy : Decoder<Energy> =
        Decode.object (fun fields -> { 
                ID = fields.Required.At [ "ID" ] Decode.string
                Amount = fields.Required.At [ "Amount" ] Decode.int
                Info = fields.Required.At [ "Info" ] Decode.string
                Created = fields.Required.At [ "Created" ] Decode.int64
                Kind = fields.Required.At [ "Kind" ] energyKind
                Place_ID = fields.Required.At [ "Place_ID" ] Decode.string
            }
        )

    let place : Decoder<Place> =
        Decode.object (fun fields -> { 
                ID = fields.Required.At [ "ID" ] Decode.string
                Name = fields.Required.At [ "Name" ] Decode.string
                CircuitBreakerCurrent = fields.Required.At [ "CircuitBreakerCurrent" ] Decode.int
            }
        )

    let provider : Decoder<Provider> =
        Decode.object (fun fields -> { 
                ID = fields.Required.At [ "ID" ] Decode.string
                Name = fields.Required.At [ "Name" ] Decode.string
            }
        )

    let product : Decoder<Product> =
        Decode.object (fun fields -> { 
                ID = fields.Required.At [ "ID" ] Decode.string
                Name = fields.Required.At [ "Name" ] Decode.string
                Provider_ID = fields.Required.At [ "Provider_ID" ] Decode.string
            }
        )

    let price : Decoder<Price> =
        Decode.object (fun fields -> { 
                ID = fields.Required.At [ "ID" ] Decode.string
                Value = fields.Required.At [ "Value" ] Decode.int
                FromDate = fields.Required.At [ "FromDate" ] Decode.string
                Provider_ID = fields.Required.At [ "Provider_ID" ] Decode.string
                PriceType = fields.Required.At [ "PriceType" ] priceType
                EnergyKind = fields.Required.At [ "EnergyKind" ] energyKind
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

    let convertbool value =
        if value then "true" else "false"

    let makeError status responseText =
        if status = 0 && responseText = "" 
        then Error($"status: {status}, network error: Unable to reach the server") 
        else Error($"status: {status}, {responseText}")

    let get url decoder = async {
        let! (status, responseText) = Http.get url
        match status with
        | 200 ->
            let items = Decode.fromString decoder responseText
            return items
        | _ ->
            return makeError status responseText
    }

    module Energies =
        let loadPagePrev (created : int64) (id: string) (limit: int) =
            get $"{url}/energies/page/prev?created={created}&id={id}&limit={limit}" (Decode.list Decode.energy)

        let loadPageNext (created : int64) (id: string) (limit: int) =
            get $"{url}/energies/page/next?created={created}&id={id}&limit={limit}" (Decode.list Decode.energy)

        let saveItem (item : Energy) = async {
            let json = Encode.energy item
            let body = Encode.toString 2 json
            let! (status, responseText) = Http.post $"{url}/energies" body
            match status with
            | 200 -> return Ok item
            | 201 -> return Ok item
            | _ -> return makeError status responseText
        }

    module Places =
        let loadPagePrev (name : string) (id: string) (limit: int) =
            get $"{url}/places/page/prev?name={name}&id={id}&limit={limit}" (Decode.list Decode.place)

        let loadPageNext (name : string) (id: string) (limit: int) =
            get $"{url}/places/page/next?name={name}&id={id}&limit={limit}" (Decode.list Decode.place)

        let loadAll () =
            get $"{url}/places" (Decode.list Decode.place)

        let saveItem (item : Place) = async {
            let json = Encode.place item
            let body = Encode.toString 2 json
            let! (status, responseText) = Http.post $"{url}/places" body
            match status with
            | 200 -> return Ok item
            | 201 -> return Ok item
            | _ -> return makeError status responseText
        }

    module Providers =
        let loadPagePrev (name : string) (id: string) (limit: int) =
            get $"{url}/providers/page/prev?name={name}&id={id}&limit={limit}" (Decode.list Decode.provider)

        let loadPageNext (name : string) (id: string) (limit: int) =
            get $"{url}/providers/page/next?name={name}&id={id}&limit={limit}" (Decode.list Decode.provider)

        let loadAll () =
            get $"{url}/providers" (Decode.list Decode.provider)

        let saveItem (item : Provider) = async {
            let json = Encode.provider item
            let body = Encode.toString 2 json
            let! (status, responseText) = Http.post $"{url}/providers" body
            match status with
            | 200 -> return Ok item
            | 201 -> return Ok item
            | _ -> return makeError status responseText
        }

    module Products =
        let loadPagePrev (name : string) (id: string) (limit: int) =
            get $"{url}/products/page/prev?name={name}&id={id}&limit={limit}" (Decode.list Decode.product)

        let loadPageNext (name : string) (id: string) (limit: int) =
            get $"{url}/products/page/next?name={name}&id={id}&limit={limit}" (Decode.list Decode.product)

        let loadAll () =
            get $"{url}/products" (Decode.list Decode.product)

        let saveItem (item : Product) = async {
            let json = Encode.product item
            let body = Encode.toString 2 json
            let! (status, responseText) = Http.post $"{url}/products" body
            match status with
            | 200 -> return Ok item
            | 201 -> return Ok item
            | _ -> return makeError status responseText
        }

    module Prices =
        let loadAll () =
            get $"{url}/prices" (Decode.list Decode.price)

        let saveItem (item : Price) = async {
            let json = Encode.price item
            let body = Encode.toString 2 json
            let! (status, responseText) = Http.post $"{url}/prices" body
            match status with
            | 200 -> return Ok item
            | 201 -> return Ok item
            | _ -> return makeError status responseText
        }

        let loadPagePrev (fromDate : string) (id: string) (limit: int) =
            get $"{url}/prices/page/prev?fromdate={fromDate}&id={id}&limit={limit}" (Decode.list Decode.price)

        let loadPageNext (fromDate : string) (id: string) (limit: int) =
            get $"{url}/prices/page/next?fromdate={fromDate}&id={id}&limit={limit}" (Decode.list Decode.price)

