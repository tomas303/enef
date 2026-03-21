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

type EnergyKind =
    | ElektricityVT
    | ElektricityNT
    | Gas
    | Water

type PriceType =
    | ComodityPerVolume
    | DistributionPerVolume
    | ComodityPerMonth
    | DistributionPerMonth
    | OTE
    | ReservedPower   //before known as electric fuse
    | POZEPerVolume
    | POZEPerMonth
    | SystemServices
    | ElectricTax
    | VAT

type Energy = {
    ID : string
    Kind: EnergyKind
    Amount : int
    Info : string
    Created : int64 
    Place_ID : string
}

type Product = {
    ID: string
    EnergyKind: EnergyKind
    PriceType: PriceType
    Provider_ID: string
    Name: string
}

type ProductPrice = {
    ID: string
    Product_ID: string
    FromDate: int64
    Value: int
}

type PlaceProduct = {
    ID: string
    FromDate: int64
    Place_ID: string
    Product_ID: string
}

type Settlement = {
    ID: string
    Date: int64
    EnergyKind: EnergyKind
    PriceType: PriceType
    Amount: int
}

type GasPriceSerie = {
    Place_ID: string
    SliceStart: int64
    SliceEnd: int64
    AmountMwh: float
    Months: float
    UnregulatedPrice: float
    RegulatedPrice: float
    TotalPrice: float
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
            ComodityPerVolume, "Comodity per volume"
            DistributionPerVolume, "Distribution per volume"
            ComodityPerMonth, "Comodity per month"
            DistributionPerMonth, "Distribution per month"
            OTE, "OTE - to market operator"
            ReservedPower, "Reserved power(earlier electric fuse)"
            POZEPerVolume, "POZE per volume"
            POZEPerMonth, "POZE per month"
            SystemServices, "System services"
            ElectricTax, "Electric tax"
            VAT, "VAT(dph)"
        ]

    let TextToPriceType = 
        Map [
            "Comodity per volume", ComodityPerVolume
            "Distribution per volume", DistributionPerVolume
            "Comodity per month", ComodityPerMonth
            "Distribution per month", DistributionPerMonth
            "OTE - to market operator", OTE
            "Reserved power(earlier electric fuse)", ReservedPower
            "POZE per volume", POZEPerVolume
            "POZE per month", POZEPerMonth
            "System services", SystemServices
            "Electric tax", ElectricTax
            "VAT(dph)", VAT
        ]

    let PriceTypeToInt = 
        Map [
            ComodityPerVolume, 1
            DistributionPerVolume, 2
            ComodityPerMonth, 3
            DistributionPerMonth, 4
            OTE, 5
            ReservedPower, 6
            POZEPerVolume, 7
            POZEPerMonth, 8
            SystemServices, 9
            ElectricTax, 10
            VAT, 11
        ]

    let IntToPriceType = 
        Map [
            1, ComodityPerVolume
            2, DistributionPerVolume
            3, ComodityPerMonth
            4, DistributionPerMonth
            5, OTE
            6, ReservedPower
            7, POZEPerVolume
            8, POZEPerMonth
            9, SystemServices
            10, ElectricTax
            11, VAT
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
        EnergyKind = EnergyKind.ElektricityNT
        PriceType = PriceType.ComodityPerVolume
        Provider_ID = ""
        Name = ""
    }

    let newProductPrice () = {
        ID = newID ()
        Product_ID = ""
        FromDate = localDateTimeToUnixTime System.DateTime.Now
        Value = 0
    }

    let newPlaceProduct () = {
        ID = newID ()
        FromDate = localDateTimeToUnixTime System.DateTime.Now
        Product_ID = ""
        Place_ID = ""
    }

    let newSettlement () = {
        ID = newID ()
        Date = localDateTimeToUnixTime System.DateTime.Now
        EnergyKind = EnergyKind.ElektricityNT
        PriceType = PriceType.ComodityPerVolume
        Amount = 0
    }

module Encode =

    let energy (ene : Energy) =
        Encode.object [
            "ID", Encode.string ene.ID
            "Kind", Encode.int ( Constants.EnergyKindToInt.[ene.Kind])
            "Amount", Encode.int ene.Amount
            "Info", Encode.string ene.Info
            "Created", Encode.int64 ene.Created
            "Place_ID", Encode.string ene.Place_ID
        ]

    let place (pl : Place) =
        Encode.object [
            "ID", Encode.string pl.ID
            "Name", Encode.string pl.Name
            "CircuitBreakerCurrent", Encode.int pl.CircuitBreakerCurrent
        ]

    let provider (pr : Provider) =
        Encode.object [
            "ID", Encode.string pr.ID
            "Name", Encode.string pr.Name
        ]

    let product (p : Product) =
        Encode.object [
            "ID", Encode.string p.ID
            "EnergyKind", Encode.int (Constants.EnergyKindToInt.[p.EnergyKind])
            "PriceType", Encode.int (Constants.PriceTypeToInt.[p.PriceType])
            "Provider_ID", Encode.string p.Provider_ID
            "Name", Encode.string p.Name
        ]

    let productprice (pp : ProductPrice) =
        Encode.object [
            "ID", Encode.string pp.ID
            "Product_ID", Encode.string pp.Product_ID
            "FromDate", Encode.int64 pp.FromDate
            "Value", Encode.int pp.Value
        ]

    let placeproduct (pp : PlaceProduct) =
        Encode.object [
            "ID", Encode.string pp.ID
            "FromDate", Encode.int64 pp.FromDate
            "Place_ID", Encode.string pp.Place_ID
            "Product_ID", Encode.string pp.Product_ID
        ]

    let settlement (s : Settlement) =
        Encode.object [
            "ID", Encode.string s.ID
            "Date", Encode.int64 s.Date
            "EnergyKind", Encode.int (Constants.EnergyKindToInt.[s.EnergyKind])
            "PriceType", Encode.int (Constants.PriceTypeToInt.[s.PriceType])
            "Amount", Encode.int s.Amount
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
                EnergyKind = fields.Required.At [ "EnergyKind" ] energyKind
                PriceType = fields.Required.At [ "PriceType" ] priceType
                Provider_ID = fields.Required.At [ "Provider_ID" ] Decode.string
                Name = fields.Required.At [ "Name" ] Decode.string
            }
        )

    let productprice : Decoder<ProductPrice> =
        Decode.object (fun fields -> { 
                ID = fields.Required.At [ "ID" ] Decode.string
                Product_ID = fields.Required.At [ "Product_ID" ] Decode.string
                FromDate = fields.Required.At [ "FromDate" ] Decode.int64
                Value = fields.Required.At [ "Value" ] Decode.int
            }
        )

    let placeproduct : Decoder<PlaceProduct> =
        Decode.object (fun fields -> { 
                ID = fields.Required.At [ "ID" ] Decode.string
                FromDate = fields.Required.At [ "FromDate" ] Decode.int64
                Place_ID = fields.Required.At [ "Place_ID" ] Decode.string
                Product_ID = fields.Required.At [ "Product_ID" ] Decode.string
            }
        )

    let gaspriceserie : Decoder<GasPriceSerie> =
        Decode.object (fun fields -> { 
                Place_ID = fields.Required.At [ "Place_ID" ] Decode.string
                SliceStart = fields.Required.At [ "SliceStart" ] Decode.int64
                SliceEnd = fields.Required.At [ "SliceEnd" ] Decode.int64
                AmountMwh = fields.Required.At [ "AmountMwh" ] Decode.float
                Months = fields.Required.At [ "Months" ] Decode.float
                UnregulatedPrice = fields.Required.At [ "UnregulatedPrice" ] Decode.float
                RegulatedPrice = fields.Required.At [ "RegulatedPrice" ] Decode.float
                TotalPrice = fields.Required.At [ "TotalPrice" ] Decode.float
            }
        )

    let settlement : Decoder<Settlement> =
        Decode.object (fun fields -> {
                ID = fields.Required.At [ "ID" ] Decode.string
                Date = fields.Required.At [ "Date" ] Decode.int64
                EnergyKind = fields.Required.At [ "EnergyKind" ] energyKind
                PriceType = fields.Required.At [ "PriceType" ] priceType
                Amount = fields.Required.At [ "Amount" ] Decode.int
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
        let ns = "products"
        let loadAll () =
            get $"{url}/{ns}" (Decode.list Decode.product)

        let saveItem (item : Product) = async {
            let json = Encode.product item
            let body = Encode.toString 2 json
            let! (status, responseText) = Http.post $"{url}/{ns}" body
            match status with
            | 200 -> return Ok item
            | 201 -> return Ok item
            | _ -> return makeError status responseText
        }

        let loadPagePrev (name : string) (id: string) (limit: int) =
            get $"{url}/{ns}/page/prev?name={name}&id={id}&limit={limit}" (Decode.list Decode.product)

        let loadPageNext (name : string) (id: string) (limit: int) =
            get $"{url}/{ns}/page/next?name={name}&id={id}&limit={limit}" (Decode.list Decode.product)

    module ProductPrices =
        let ns = "productprices"
        let loadAll () =
            get $"{url}/{ns}" (Decode.list Decode.productprice)

        let saveItem (item : ProductPrice) = async {
            let json = Encode.productprice item
            let body = Encode.toString 2 json
            let! (status, responseText) = Http.post $"{url}/{ns}" body
            match status with
            | 200 -> return Ok item
            | 201 -> return Ok item
            | _ -> return makeError status responseText
        }

        let loadPagePrev (fromdate: int64) (id: string) (limit: int) =
            get $"{url}/{ns}/page/prev?fromdate={fromdate}&id={id}&limit={limit}" (Decode.list Decode.productprice)

        let loadPageNext (fromdate: int64) (id: string) (limit: int) =
            get $"{url}/{ns}/page/next?fromdate={fromdate}&id={id}&limit={limit}" (Decode.list Decode.productprice)

    module PlaceProducts =
        let ns = "placeproducts"
        let loadAll () =
            get $"{url}/{ns}" (Decode.list Decode.placeproduct)

        let saveItem (item : PlaceProduct) = async {
            let json = Encode.placeproduct item
            let body = Encode.toString 2 json
            let! status, responseText = Http.post $"{url}/{ns}" body
            match status with
            | 200 -> return Ok item
            | 201 -> return Ok item
            | _ -> return makeError status responseText
        }

        let loadPagePrev (fromdate: int64) (id: string) (limit: int) =
            get $"{url}/{ns}/page/prev?fromdate={fromdate}&id={id}&limit={limit}" (Decode.list Decode.placeproduct)

        let loadPageNext (fromdate: int64) (id: string) (limit: int) =
            get $"{url}/{ns}/page/next?fromdate={fromdate}&id={id}&limit={limit}" (Decode.list Decode.placeproduct)

    module PriceSeries =
        let loadGas  (fromdate : int64) (todate: int64) =
            get $"{url}/gas-prices?fromdate={fromdate}&todate={todate}" (Decode.list Decode.gaspriceserie)

    module Settlements =
        let ns = "settlements"

        let saveItem (item : Settlement) = async {
            let json = Encode.settlement item
            let body = Encode.toString 2 json
            let! (status, responseText) = Http.post $"{url}/{ns}" body
            match status with
            | 200 -> return Ok item
            | 201 -> return Ok item
            | _ -> return makeError status responseText
        }

        let loadPagePrev (date: int64) (id: string) (limit: int) =
            get $"{url}/{ns}/page/prev?date={date}&id={id}&limit={limit}" (Decode.list Decode.settlement)

        let loadPageNext (date: int64) (id: string) (limit: int) =
            get $"{url}/{ns}/page/next?date={date}&id={id}&limit={limit}" (Decode.list Decode.settlement)

