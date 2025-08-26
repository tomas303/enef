// x-text.js
import { XInputBase } from './x-input-base.js';

class XText extends XInputBase {
  constructor() {
    super();
    console.log('XText constructor called');
  }

  _createStructure(shadow) {
    console.log('XText _createStructure called');
    // wrapper for positioning
    this._wrapper = document.createElement('div');
    this._wrapper.className = 'wrapper';

    // editor
    this._editor = document.createElement('div');
    this._editor.setAttribute('role', 'textbox');
    this._editor.tabIndex = 0;
    this._editor.contentEditable = 'true';
    this._editor.className = 'editable';

    this._wrapper.append(this._editor);
    shadow.append(this._wrapper);
    
    console.log('XText structure created:', {
      wrapper: !!this._wrapper,
      editor: !!this._editor
    });
  }

  _addEventListeners() {
    this._editor.addEventListener('input', this._onInput);
    this._editor.addEventListener('blur', this._onBlur);
    this._editor.addEventListener('keydown', this._onKeyDown);
    this._editor.addEventListener('paste', this._onPaste);
  }

  _removeEventListeners() {
    this._editor.removeEventListener('input', this._onInput);
    this._editor.removeEventListener('blur', this._onBlur);
    this._editor.removeEventListener('keydown', this._onKeyDown);
    this._editor.removeEventListener('paste', this._onPaste);
  }

  _getEditorValue() {
    return this._editor.innerText;
  }

  _setEditorValue(value) {
    this._editor.innerText = value;
  }

  _hasFocus() {
    return this._editor.matches(':focus');
  }

  _setDisabled(disabled) {
    this._editor.contentEditable = disabled ? 'false' : 'true';
    this._editor.tabIndex = disabled ? -1 : 0;
    this._editor.setAttribute('aria-disabled', String(disabled));
  }

  focus() {
    this._editor.focus();
  }

  blur() {
    this._editor.blur();
  }
}

customElements.define('x-text', XText);
