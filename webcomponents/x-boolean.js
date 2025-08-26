// x-boolean.js
import { XInputBase } from './x-input-base.js';

class XBoolean extends XInputBase {
  constructor() {
    super();
  }

  static get observedAttributes() {
    return ['value', 'disabled'];
  }

  _createStructure(shadow) {
    // wrapper for positioning
    this._wrapper = document.createElement('div');
    this._wrapper.className = 'wrapper';

    // editor
    this._editor = document.createElement('div');
    this._editor.setAttribute('role', 'button');
    this._editor.tabIndex = 0;
    this._editor.className = 'editable boolean-display';
    this._editor.setAttribute('aria-label', 'Boolean value');

    this._wrapper.append(this._editor);
    shadow.append(this._wrapper);
  }

  _getCustomStyles() {
    return `
      .boolean-display {
        min-width: 2ch;
        max-width: 2ch;
        min-height: 1.5em;
        padding: 0.5em;
        border: 1px solid #ccc;
        border-radius: 4px;
        outline: none;
        box-sizing: border-box;
        font: inherit;
        line-height: 1.2;
        text-align: center;
        cursor: pointer;
        user-select: none;
        font-weight: bold;
        display: flex;
        align-items: center;
        justify-content: center;
        background: white;
        color: inherit;
      }
      .boolean-display:focus {
        box-shadow: 0 0 0 3px rgba(100,150,250,0.12);
      }
      .boolean-display:hover:not([aria-disabled="true"]) {
        opacity: 0.8;
      }
      .boolean-display[aria-disabled="true"] {
        opacity: 0.5;
        cursor: not-allowed;
      }
    `;
  }

  _addEventListeners() {
    this._editor.addEventListener('click', this._onClick);
    this._editor.addEventListener('keydown', this._onKeyDown);
    this._editor.addEventListener('blur', this._onBlur);
  }

  _removeEventListeners() {
    this._editor.removeEventListener('click', this._onClick);
    this._editor.removeEventListener('keydown', this._onKeyDown);
    this._editor.removeEventListener('blur', this._onBlur);
  }

  _getEditorValue() {
    return this._editor.classList.contains('true') ? 'true' : 'false';
  }

  _setEditorValue(value) {
    const boolValue = this._parseBoolean(value);
    this._updateDisplay(boolValue);
  }

  _parseBoolean(value) {
    if (typeof value === 'boolean') return value;
    if (typeof value === 'string') {
      const lower = value.toLowerCase().trim();
      return lower === 'true' || lower === '1' || lower === 'yes' || lower === 'on';
    }
    return Boolean(value);
  }

  _updateDisplay(boolValue) {
    this._editor.classList.remove('true', 'false');
    this._editor.classList.add(boolValue ? 'true' : 'false');
    this._editor.textContent = boolValue ? 'O' : 'X';
    this._editor.setAttribute('aria-pressed', String(boolValue));
  }

  _hasFocus() {
    return this._editor.matches(':focus');
  }

  _setDisabled(disabled) {
    this._editor.tabIndex = disabled ? -1 : 0;
    this._editor.setAttribute('aria-disabled', String(disabled));
  }

  _validateInput(value) {
    return this._parseBoolean(value) ? 'true' : 'false';
  }

  _onClick = (e) => {
    if (this._editor.getAttribute('aria-disabled') === 'true') return;
    
    this._toggle();
  };

  _onKeyDown = (e) => {
    if (this._editor.getAttribute('aria-disabled') === 'true') return;
    
    // Handle specific keys
    if (e.key === 'Escape') {
      e.preventDefault();
      // Restore to last committed value
      this._setEditorValue(this._lastValue);
      this.setAttribute('value', this._lastValue);
      this.blur();
    } else if (e.key.length === 1 || e.key === ' ' || e.key === 'Enter') {
      // Any key press (including space/enter) toggles the value
      e.preventDefault();
      this._toggle();
    }
  };

  _toggle() {
    const currentValue = this._getEditorValue() === 'true';
    this._setValue(!currentValue);
  }

  _setValue(boolValue) {
    const newValue = boolValue ? 'true' : 'false';
    
    this._updateDisplay(boolValue);
    
    if (this.getAttribute('value') !== newValue) {
      this.setAttribute('value', newValue);
    }
    
    this.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true }));
  }

  focus() {
    this._editor.focus();
  }

  blur() {
    this._editor.blur();
  }

  // JS property getter/setter for .value - return actual boolean
  get value() {
    return this._getEditorValue() === 'true';
  }

  set value(val) {
    const boolVal = this._parseBoolean(val);
    this.setAttribute('value', boolVal ? 'true' : 'false');
  }
}

customElements.define('x-boolean', XBoolean);