import { Union, Record } from "../fable_modules/fable-library-js.4.16.0/Types.js";
import { render as render_1, update as update_1, stateToEnergy, empty, Msg_$reflection as Msg_$reflection_1, State_$reflection as State_$reflection_1 } from "./WgEdit.fs.js";
import { Render_gridRow, Render_grid, Api_saveItem, Api_loadLastRows, Energy_$reflection } from "./Lib.fs.js";
import { union_type, string_type, record_type, list_type } from "../fable_modules/fable-library-js.4.16.0/Reflection.js";
import { Async_map, Cmd_fromAsync, AsyncOperationEvent$1, Deferred$1, AsyncOperationEvent$1_$reflection, Deferred$1_$reflection } from "../Extensions.fs.js";
import { FSharpResult$2 } from "../fable_modules/fable-library-js.4.16.0/Result.js";
import { ofArray, collect, skip, length, append, singleton, empty as empty_1 } from "../fable_modules/fable-library-js.4.16.0/List.js";
import { Cmd_none } from "../fable_modules/Fable.Elmish.4.0.0/cmd.fs.js";
import { createElement } from "react";
import { createObj } from "../fable_modules/fable-library-js.4.16.0/Util.js";
import { join } from "../fable_modules/fable-library-js.4.16.0/String.js";
import { Interop_reactApi } from "../fable_modules/Feliz.2.7.0/./Interop.fs.js";

export class State extends Record {
    constructor(WgEditSt, LastRows, LastEdits) {
        super();
        this.WgEditSt = WgEditSt;
        this.LastRows = LastRows;
        this.LastEdits = LastEdits;
    }
}

export function State_$reflection() {
    return record_type("Energies.WgAddNew.State", [], State, () => [["WgEditSt", State_$reflection_1()], ["LastRows", Deferred$1_$reflection(list_type(Energy_$reflection()))], ["LastEdits", list_type(Energy_$reflection())]]);
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["WgEditMsg", "InitPage", "LoadLastRows", "SaveItem"];
    }
}

export function Msg_$reflection() {
    return union_type("Energies.WgAddNew.Msg", [], Msg, () => [[["Item", Msg_$reflection_1()]], [], [["Item", AsyncOperationEvent$1_$reflection(union_type("Microsoft.FSharp.Core.FSharpResult`2", [list_type(Energy_$reflection()), string_type], FSharpResult$2, () => [[["ResultValue", list_type(Energy_$reflection())]], [["ErrorValue", string_type]]]))]], [["Item", AsyncOperationEvent$1_$reflection(union_type("Microsoft.FSharp.Core.FSharpResult`2", [Energy_$reflection(), string_type], FSharpResult$2, () => [[["ResultValue", Energy_$reflection()]], [["ErrorValue", string_type]]]))]]]);
}

export function init() {
    return new State(empty(), new Deferred$1(0, []), empty_1());
}

export function update(msg, state) {
    switch (msg.tag) {
        case 1:
            return [state, singleton((dispatch) => {
                dispatch(new Msg(2, [new AsyncOperationEvent$1(0, [])]));
            })];
        case 2: {
            const x_1 = msg.fields[0];
            if (x_1.tag === 1) {
                if (x_1.fields[0].tag === 1) {
                    const text = x_1.fields[0].fields[0];
                    const state_3 = new State(state.WgEditSt, new Deferred$1(2, [empty_1()]), state.LastEdits);
                    return [state_3, Cmd_none()];
                }
                else {
                    const items = x_1.fields[0].fields[0];
                    const state_2 = new State(state.WgEditSt, new Deferred$1(2, [items]), items);
                    return [state_2, Cmd_none()];
                }
            }
            else {
                const state_1 = new State(state.WgEditSt, new Deferred$1(1, []), state.LastEdits);
                return [state_1, Cmd_fromAsync(Async_map((x_2) => (new Msg(2, [new AsyncOperationEvent$1(1, [x_2])])), Api_loadLastRows))];
            }
        }
        case 3: {
            const x_3 = msg.fields[0];
            if (x_3.tag === 1) {
                if (x_3.fields[0].tag === 1) {
                    const text_1 = x_3.fields[0].fields[0];
                    return [state, Cmd_none()];
                }
                else {
                    const item = x_3.fields[0].fields[0];
                    const lastedits = append(state.LastEdits, singleton(item));
                    const lastedits2 = (length(lastedits) > 10) ? skip(length(lastedits) - 10, lastedits) : lastedits;
                    return [new State(state.WgEditSt, state.LastRows, lastedits2), Cmd_none()];
                }
            }
            else {
                const asyncSave = Api_saveItem(stateToEnergy(state.WgEditSt));
                return [state, Cmd_fromAsync(Async_map((x_4) => (new Msg(3, [new AsyncOperationEvent$1(1, [x_4])])), asyncSave))];
            }
        }
        default: {
            const x = msg.fields[0];
            const patternInput = update_1(x, state.WgEditSt);
            const newstate = patternInput[0];
            const newcmd = patternInput[1];
            return [new State(newstate, state.LastRows, state.LastEdits), newcmd];
        }
    }
}

export function render(state, dispatch) {
    let elems_1, elems;
    const grid = Render_grid(() => collect(Render_gridRow, state.LastEdits));
    const edit = render_1(state.WgEditSt, (x) => {
        dispatch(new Msg(0, [x]));
    });
    const addButton = createElement("div", createObj(ofArray([["className", join(" ", ["columns"])], (elems_1 = [createElement("div", createObj(ofArray([["className", join(" ", ["column"])], (elems = [createElement("button", {
        className: join(" ", ["button", "is-primary", "is-pulled-right"]),
        children: "Add",
        onClick: (_arg) => {
            dispatch(new Msg(3, [new AsyncOperationEvent$1(0, [])]));
        },
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])));
    return ofArray([grid, edit, addButton]);
}

