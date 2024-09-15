import { FSharpRef, Union, Record } from "../fable_modules/fable-library-js.4.16.0/Types.js";
import { union_type, float64_type, record_type, class_type, int32_type, string_type } from "../fable_modules/fable-library-js.4.16.0/Reflection.js";
import { Render_edit, Render_Inputs, Constants_EnergyKindToText, Constants_EnergyKindToInt, Utils_intToEnergyKind, Utils_jsTimestampToDateTime, Utils_joinDateAndTime, EnergyKind, Energy, Utils_localDateTimeToUnixTime, EnergyKind_$reflection } from "./Lib.fs.js";
import { newGuid } from "../fable_modules/fable-library-js.4.16.0/Guid.js";
import { toString, now } from "../fable_modules/fable-library-js.4.16.0/Date.js";
import { Cmd_none } from "../fable_modules/Fable.Elmish.4.0.0/cmd.fs.js";
import { tryParse } from "../fable_modules/fable-library-js.4.16.0/Int32.js";
import { map } from "../fable_modules/fable-library-js.4.16.0/List.js";
import { createElement } from "react";
import { toList, FSharpMap__get_Item } from "../fable_modules/fable-library-js.4.16.0/Map.js";
import { join } from "../fable_modules/fable-library-js.4.16.0/String.js";
import { Interop_reactApi } from "../fable_modules/Feliz.2.7.0/./Interop.fs.js";
import { int32ToString } from "../fable_modules/fable-library-js.4.16.0/Util.js";

export class State extends Record {
    constructor(ID, Kind, Amount, Info, Created) {
        super();
        this.ID = ID;
        this.Kind = Kind;
        this.Amount = (Amount | 0);
        this.Info = Info;
        this.Created = Created;
    }
}

export function State_$reflection() {
    return record_type("Energies.WgEdit.State", [], State, () => [["ID", string_type], ["Kind", EnergyKind_$reflection()], ["Amount", int32_type], ["Info", string_type], ["Created", class_type("System.DateTime")]]);
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Date", "Time", "Kind", "Amount", "Info"];
    }
}

export function Msg_$reflection() {
    return union_type("Energies.WgEdit.Msg", [], Msg, () => [[["Item", float64_type]], [["Item", float64_type]], [["Item", string_type]], [["Item", string_type]], [["Item", string_type]]]);
}

export function stateToEnergy(state) {
    return new Energy(state.ID, state.Kind, state.Amount, state.Info, Utils_localDateTimeToUnixTime(state.Created));
}

export function empty() {
    let copyOfStruct;
    return new State((copyOfStruct = newGuid(), copyOfStruct), new EnergyKind(2, []), 0, "", now());
}

export function update(msg, state) {
    switch (msg.tag) {
        case 1: {
            const x_1 = msg.fields[0];
            const created_1 = Utils_joinDateAndTime(state.Created, Utils_jsTimestampToDateTime(x_1));
            const state_2 = new State(state.ID, state.Kind, state.Amount, state.Info, created_1);
            return [state_2, Cmd_none()];
        }
        case 2: {
            const x_2 = msg.fields[0];
            let matchValue;
            let outArg = 0;
            matchValue = [tryParse(x_2, 511, false, 32, new FSharpRef(() => outArg, (v) => {
                outArg = (v | 0);
            })), outArg];
            if (matchValue[0]) {
                const intValue = matchValue[1] | 0;
                let state_3;
                const matchValue_1 = Utils_intToEnergyKind(intValue);
                if (matchValue_1 == null) {
                    state_3 = state;
                }
                else {
                    const x_3 = matchValue_1;
                    state_3 = (new State(state.ID, x_3, state.Amount, state.Info, state.Created));
                }
                return [state_3, Cmd_none()];
            }
            else {
                return [state, Cmd_none()];
            }
        }
        case 3: {
            const x_4 = msg.fields[0];
            let state_4;
            let matchValue_2;
            let outArg_1 = 0;
            matchValue_2 = [tryParse(x_4, 511, false, 32, new FSharpRef(() => outArg_1, (v_1) => {
                outArg_1 = (v_1 | 0);
            })), outArg_1];
            if (matchValue_2[0]) {
                const amount = matchValue_2[1] | 0;
                state_4 = (new State(state.ID, state.Kind, amount, state.Info, state.Created));
            }
            else {
                state_4 = state;
            }
            return [state_4, Cmd_none()];
        }
        case 4: {
            const x_5 = msg.fields[0];
            const state_5 = new State(state.ID, state.Kind, state.Amount, x_5, state.Created);
            return [state_5, Cmd_none()];
        }
        default: {
            const x = msg.fields[0];
            const created = Utils_joinDateAndTime(Utils_jsTimestampToDateTime(x), state.Created);
            const state_1 = new State(state.ID, state.Kind, state.Amount, state.Info, created);
            return [state_1, Cmd_none()];
        }
    }
}

function renderInputs(state, dispatch) {
    const kindSelectOptions = map((tupledArg) => {
        const energyKind = tupledArg[0];
        const text = tupledArg[1];
        return createElement("option", {
            children: text,
            value: FSharpMap__get_Item(Constants_EnergyKindToInt, energyKind),
        });
    }, toList(Constants_EnergyKindToText));
    return new Render_Inputs(createElement("input", {
        className: join(" ", ["input"]),
        type: "date",
        value: toString(state.Created, "yyyy-MM-dd"),
        onChange: (ev) => {
            const value_9 = ev.target.valueAsNumber;
            if (!(value_9 == null) && !Number.isNaN(value_9)) {
                dispatch(new Msg(0, [value_9]));
            }
        },
    }), createElement("input", {
        className: join(" ", ["input"]),
        type: "time",
        value: toString(state.Created, "HH:mm"),
        onChange: (ev_1) => {
            const value_16 = ev_1.target.valueAsNumber;
            if (!(value_16 == null) && !Number.isNaN(value_16)) {
                dispatch(new Msg(1, [value_16]));
            }
        },
    }), createElement("select", {
        children: Interop_reactApi.Children.toArray(Array.from(kindSelectOptions)),
        onChange: (ev_2) => {
            dispatch(new Msg(2, [ev_2.target.value]));
        },
    }), createElement("input", {
        className: join(" ", ["input"]),
        type: "text",
        placeholder: "amount",
        value: int32ToString(state.Amount),
        onChange: (ev_3) => {
            dispatch(new Msg(3, [ev_3.target.value]));
        },
    }), createElement("input", {
        className: join(" ", ["input"]),
        type: "text",
        placeholder: "remark",
        value: state.Info,
        onChange: (ev_4) => {
            dispatch(new Msg(4, [ev_4.target.value]));
        },
    }));
}

export function render(state, dispatch) {
    const inputs = renderInputs(state, dispatch);
    return Render_edit(inputs);
}

