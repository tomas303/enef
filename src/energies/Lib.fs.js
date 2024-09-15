import { Record, Union } from "../fable_modules/fable-library-js.4.16.0/Types.js";
import { class_type, record_type, int64_type, int32_type, string_type, union_type } from "../fable_modules/fable-library-js.4.16.0/Reflection.js";
import { FSharpMap__get_Item, containsKey, ofSeq } from "../fable_modules/fable-library-js.4.16.0/Map.js";
import { createObj, uncurry2, round, comparePrimitives, compare } from "../fable_modules/fable-library-js.4.16.0/Util.js";
import { fromUnixTimeMilliseconds, toLocalTime, fromUnixTimeSeconds } from "../fable_modules/fable-library-js.4.16.0/DateOffset.js";
import { toString as toString_1, second, minute, hour, day, month, year, fromDateTimeOffset, toUniversalTime, op_Subtraction, create } from "../fable_modules/fable-library-js.4.16.0/Date.js";
import { fromInt32, op_Division, fromFloat64, toInt64 } from "../fable_modules/fable-library-js.4.16.0/BigInt.js";
import { seconds, minutes, hours, ticks, totalSeconds } from "../fable_modules/fable-library-js.4.16.0/TimeSpan.js";
import { day as day_1, month as month_1, year as year_1, create as create_1 } from "../fable_modules/fable-library-js.4.16.0/DateOnly.js";
import { create as create_2 } from "../fable_modules/fable-library-js.4.16.0/TimeOnly.js";
import { toString, int64, object } from "../fable_modules/Thoth.Json.10.2.0/Encode.fs.js";
import { ErrorReason } from "../fable_modules/Thoth.Json.10.2.0/Types.fs.js";
import { FSharpResult$2 } from "../fable_modules/fable-library-js.4.16.0/Result.js";
import { list, fromString, int64 as int64_1, int, string, object as object_1 } from "../fable_modules/Thoth.Json.10.2.0/Decode.fs.js";
import { ofArray, singleton } from "../fable_modules/fable-library-js.4.16.0/List.js";
import { singleton as singleton_1 } from "../fable_modules/fable-library-js.4.16.0/AsyncBuilder.js";
import { Http_post, Http_get } from "../fable_modules/Fable.SimpleHttp.3.6.0/Http.fs.js";
import { createElement } from "react";
import { join } from "../fable_modules/fable-library-js.4.16.0/String.js";
import { Interop_reactApi } from "../fable_modules/Feliz.2.7.0/./Interop.fs.js";

export class EnergyKind extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["ElektricityVT", "ElektricityNT", "Gas", "Water"];
    }
}

export function EnergyKind_$reflection() {
    return union_type("Energies.EnergyKind", [], EnergyKind, () => [[], [], [], []]);
}

export class Energy extends Record {
    constructor(ID, Kind, Amount, Info, Created) {
        super();
        this.ID = ID;
        this.Kind = Kind;
        this.Amount = (Amount | 0);
        this.Info = Info;
        this.Created = Created;
    }
}

export function Energy_$reflection() {
    return record_type("Energies.Energy", [], Energy, () => [["ID", string_type], ["Kind", EnergyKind_$reflection()], ["Amount", int32_type], ["Info", string_type], ["Created", int64_type]]);
}

export const Constants_EnergyKindToUnit = ofSeq([[new EnergyKind(0, []), "kWh"], [new EnergyKind(1, []), "kWh"], [new EnergyKind(2, []), "m3"], [new EnergyKind(3, []), "m3"]], {
    Compare: compare,
});

export const Constants_EnergyKindToText = ofSeq([[new EnergyKind(0, []), "Electricity VT"], [new EnergyKind(1, []), "Electricity NT"], [new EnergyKind(2, []), "Gas"], [new EnergyKind(3, []), "Water"]], {
    Compare: compare,
});

export const Constants_EnergyKindToInt = ofSeq([[new EnergyKind(0, []), 1], [new EnergyKind(1, []), 2], [new EnergyKind(2, []), 3], [new EnergyKind(3, []), 4]], {
    Compare: compare,
});

export const Constants_IntToEnergyKind = ofSeq([[1, new EnergyKind(0, [])], [2, new EnergyKind(1, [])], [3, new EnergyKind(2, [])], [4, new EnergyKind(3, [])]], {
    Compare: comparePrimitives,
});

export function Utils_unixTimeToLocalDateTime(unixTimeSeconds) {
    const dateTimeOffset = fromUnixTimeSeconds(unixTimeSeconds);
    return toLocalTime(toLocalTime(dateTimeOffset));
}

export function Utils_localDateTimeToUnixTime(datetime) {
    const unixEpoch = create(1970, 1, 1, 0, 0, 0, 0, 1);
    const timeSpan = op_Subtraction(toUniversalTime(datetime), unixEpoch);
    return toInt64(fromFloat64(round(totalSeconds(timeSpan))));
}

export function Utils_toUnixTimeSeconds(dateTime) {
    const unixEpoch = create(1970, 1, 1, 0, 0, 0, 0, 1);
    const timeSpan = op_Subtraction(dateTime, unixEpoch);
    return toInt64(op_Division(ticks(timeSpan), toInt64(fromInt32(10000000))));
}

export function Utils_jsTimestampToDateTime(timestamp) {
    const dateTimeOffset = fromUnixTimeMilliseconds(toInt64(fromFloat64(timestamp)));
    return fromDateTimeOffset(dateTimeOffset, 0);
}

export function Utils_joinDateAndTime(date, time) {
    const dateo = create_1(year(date), month(date), day(date));
    const timeo = create_2(hour(time), minute(time), second(time));
    const dateTime = create(year_1(dateo), month_1(dateo), day_1(dateo), hours(timeo), minutes(timeo), seconds(timeo));
    return dateTime;
}

export function Utils_intToEnergyKind(value) {
    let x;
    if ((x = (value | 0), containsKey(value, Constants_IntToEnergyKind))) {
        const x_1 = value | 0;
        return FSharpMap__get_Item(Constants_IntToEnergyKind, x_1);
    }
    else {
        return void 0;
    }
}

export function Encode_energy(ene) {
    return object([["ID", ene.ID], ["Kind", FSharpMap__get_Item(Constants_EnergyKindToInt, ene.Kind)], ["Amount", ene.Amount], ["Info", ene.Info], ["Created", int64(ene.Created)]]);
}

export function Decode_energyKind(path, value) {
    if ((typeof value) === "number") {
        const value_1 = value | 0;
        const matchValue = Utils_intToEnergyKind(value_1);
        if (matchValue == null) {
            return new FSharpResult$2(1, [[path, new ErrorReason(0, ["int value mapping kind out of range", value_1])]]);
        }
        else {
            const x = matchValue;
            return new FSharpResult$2(0, [x]);
        }
    }
    else {
        return new FSharpResult$2(1, [[path, new ErrorReason(0, ["value mapping kind is not a number", value])]]);
    }
}

export const Decode_energy = (path_3) => ((v) => object_1((fields) => {
    let objectArg_4;
    let ID;
    const objectArg = fields.Required;
    ID = objectArg.At(singleton("ID"), string);
    let Amount;
    const objectArg_1 = fields.Required;
    Amount = objectArg_1.At(singleton("Amount"), uncurry2(int));
    let Info;
    const objectArg_2 = fields.Required;
    Info = objectArg_2.At(singleton("Info"), string);
    let Created;
    const objectArg_3 = fields.Required;
    Created = objectArg_3.At(singleton("Created"), uncurry2(int64_1));
    return new Energy(ID, (objectArg_4 = fields.Required, objectArg_4.At(singleton("Kind"), Decode_energyKind)), Amount, Info, Created);
}, path_3, v));

export const Api_loadItems = singleton_1.Delay(() => singleton_1.Bind(Http_get("http://localhost:8085/energies"), (_arg) => {
    const status = _arg[0] | 0;
    const responseText = _arg[1];
    if (status === 200) {
        const items = fromString((path, value) => list(uncurry2(Decode_energy), path, value), responseText);
        if (items.tag === 1) {
            const parseError = items.fields[0];
            return singleton_1.Return(new FSharpResult$2(1, [parseError]));
        }
        else {
            const x = items.fields[0];
            return singleton_1.Return(new FSharpResult$2(0, [x]));
        }
    }
    else {
        return singleton_1.Return(new FSharpResult$2(1, [responseText]));
    }
}));

export const Api_loadLastRows = singleton_1.Delay(() => singleton_1.Bind(Http_get("http://localhost:8085/lastenergies?count=10"), (_arg) => {
    const status = _arg[0] | 0;
    const responseText = _arg[1];
    if (status === 200) {
        const items = fromString((path, value) => list(uncurry2(Decode_energy), path, value), responseText);
        if (items.tag === 1) {
            const parseError = items.fields[0];
            return singleton_1.Return(new FSharpResult$2(1, [parseError]));
        }
        else {
            const x = items.fields[0];
            return singleton_1.Return(new FSharpResult$2(0, [x]));
        }
    }
    else {
        return singleton_1.Return(new FSharpResult$2(1, [responseText]));
    }
}));

export function Api_saveItem(item) {
    return singleton_1.Delay(() => {
        const json = Encode_energy(item);
        const body = toString(2, json);
        return singleton_1.Bind(Http_post("http://localhost:8085/energies", body), (_arg) => {
            const status = _arg[0] | 0;
            const responseText = _arg[1];
            return (status === 200) ? singleton_1.Return(new FSharpResult$2(0, [item])) : ((status === 201) ? singleton_1.Return(new FSharpResult$2(0, [item])) : singleton_1.Return(new FSharpResult$2(1, [responseText])));
        });
    });
}

export class Render_Inputs extends Record {
    constructor(date, time, kind, amount, info) {
        super();
        this.date = date;
        this.time = time;
        this.kind = kind;
        this.amount = amount;
        this.info = info;
    }
}

export function Render_Inputs_$reflection() {
    return record_type("Energies.Render.Inputs", [], Render_Inputs, () => [["date", class_type("Fable.React.ReactElement")], ["time", class_type("Fable.React.ReactElement")], ["kind", class_type("Fable.React.ReactElement")], ["amount", class_type("Fable.React.ReactElement")], ["info", class_type("Fable.React.ReactElement")]]);
}

export function Render_gridRow(item) {
    const kind = FSharpMap__get_Item(Constants_EnergyKindToText, item.Kind);
    const created = toString_1(Utils_unixTimeToLocalDateTime(item.Created), "dd.MM.yyyy HH:mm");
    const amount = `${item.Amount} ${FSharpMap__get_Item(Constants_EnergyKindToUnit, item.Kind)}`;
    return ofArray([createElement("div", {
        className: join(" ", ["cell"]),
        children: Interop_reactApi.Children.toArray([kind]),
    }), createElement("div", {
        className: join(" ", ["cell"]),
        children: Interop_reactApi.Children.toArray([created]),
    }), createElement("div", {
        className: join(" ", ["cell"]),
        children: Interop_reactApi.Children.toArray([amount]),
    }), createElement("div", {
        className: join(" ", ["cell"]),
        children: Interop_reactApi.Children.toArray([item.Info]),
    })]);
}

export function Render_grid(renderRows) {
    let elems_3, elems_2, elems_1, elems;
    return createElement("div", createObj(ofArray([["className", join(" ", ["columns"])], (elems_3 = [createElement("div", createObj(ofArray([["className", join(" ", ["column"])], (elems_2 = [createElement("div", createObj(ofArray([["className", join(" ", ["fixed-grid", "has-1-cols-mobile", "has-2-cols-tablet", "has-4-cols-desktop"])], (elems_1 = [createElement("div", createObj(ofArray([["className", join(" ", ["grid is-gap-0"])], (elems = renderRows(), ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])])));
}

export function Render_edit(inputs) {
    let elems_13, elems_5, elems_4, elems_1, elems_9, elems_8, elems_7, elems_12, elems_11;
    return createElement("div", createObj(ofArray([["className", join(" ", ["column"])], (elems_13 = [createElement("div", createObj(ofArray([["className", join(" ", ["column"])], ["style", {
        backgroundColor: "red",
    }], (elems_5 = [createElement("div", createObj(ofArray([["className", join(" ", ["field", "has-addons"])], (elems_4 = [createElement("div", createObj(ofArray([["className", join(" ", ["control"])], (elems_1 = [createElement("div", {
        className: join(" ", ["select"]),
        children: Interop_reactApi.Children.toArray([inputs.kind]),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))), createElement("div", {
        className: join(" ", ["control"]),
        children: Interop_reactApi.Children.toArray([inputs.date]),
    }), createElement("div", {
        className: join(" ", ["control"]),
        children: Interop_reactApi.Children.toArray([inputs.time]),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])]))), createElement("div", createObj(ofArray([["className", join(" ", ["column", "is-one-fifth"])], ["style", {
        backgroundColor: "blue",
    }], (elems_9 = [createElement("div", createObj(ofArray([["className", join(" ", ["field has-addons"])], (elems_8 = [createElement("div", {
        className: join(" ", ["control is-expanded"]),
        children: Interop_reactApi.Children.toArray([inputs.amount]),
    }), createElement("div", createObj(ofArray([["className", join(" ", ["control"])], (elems_7 = [createElement("a", {
        className: join(" ", ["button is-static"]),
        children: "kWh",
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_7))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_8))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_9))])]))), createElement("div", createObj(ofArray([["className", join(" ", ["column"])], ["style", {
        backgroundColor: "lime",
    }], (elems_12 = [createElement("div", createObj(ofArray([["className", join(" ", ["field"])], (elems_11 = [createElement("div", {
        className: join(" ", ["control", "is-expanded"]),
        children: Interop_reactApi.Children.toArray([inputs.info]),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_11))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_12))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_13))])])));
}

