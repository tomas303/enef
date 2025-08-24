// x-text.js
class XText extends HTMLElement {
  constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });

    // wrapper for positioning placeholder
    const wrapper = document.createElement('div');
    wrapper.className = 'wrapper';

    // editable surface (no native <input>)
    this._editor = document.createElement('div');
    this._editor.setAttribute('role', 'textbox');
    this._editor.tabIndex = 0;
    this._editor.contentEditable = 'true';
    this._editor.className = 'editable';

    // placeholder element
    this._placeholder = document.createElement('div');
    this._placeholder.className = 'placeholder';
    this._placeholder.setAttribute('aria-hidden', 'true');

    const style = document.createElement('style');
    style.textContent = `
      :host { display: inline-block; font: inherit; position: relative; }
      .wrapper { position: relative; }
      .editable {
        min-width: 6ch;
        min-height: 1.5em;
        padding: 0.25em;
        border: 1px solid #ccc;
        border-radius: 4px;
        outline: none;
        white-space: pre-wrap;
        spellcheck: false;
      }
      .editable:focus { box-shadow: 0 0 0 3px rgba(100,150,250,0.12); }
      .placeholder {
        position: absolute;
        left: 0.25em;
        top: 0.25em;
        color: #888;
        pointer-events: none;
        user-select: none;
      }
    `;

    wrapper.append(this._placeholder, this._editor);
    shadow.append(style, wrapper);

    // track last dispatched value for change detection
    this._lastValue = '';
  }

  connectedCallback() {
    // Initialize from attributes
    this._syncFromAttribute('value');
    this._syncFromAttribute('placeholder');
    this._syncFromAttribute('disabled');

    // Add event listeners
    this._editor.addEventListener('input', this._onInput);
    this._editor.addEventListener('blur', this._onBlur);
    this._editor.addEventListener('keydown', this._onKeyDown);
    this._editor.addEventListener('paste', this._onPaste);
  }

  disconnectedCallback() {
    this._editor.removeEventListener('input', this._onInput);
    this._editor.removeEventListener('blur', this._onBlur);
    this._editor.removeEventListener('keydown', this._onKeyDown);
    this._editor.removeEventListener('paste', this._onPaste);
  }

  static get observedAttributes() {
    return ['value', 'disabled', 'placeholder'];
  }

  attributeChangedCallback(name, oldValue, newValue) {
    this._syncFromAttribute(name);
  }

  // internal: keep placeholder/editor/disabled in sync
  _syncFromAttribute(name) {
    if (name === 'value') {
      const v = this.getAttribute('value') || '';
      if (this._editor.innerText !== v) this._editor.innerText = v;
      this._updatePlaceholder();
      this._lastValue = this._editor.innerText;
    }
    if (name === 'placeholder') {
      this._placeholder.textContent = this.getAttribute('placeholder') || '';
      this._updatePlaceholder();
    }
    if (name === 'disabled') {
      const disabled = this.hasAttribute('disabled');
      this._editor.contentEditable = disabled ? 'false' : 'true';
      this._editor.tabIndex = disabled ? -1 : 0;
      this._editor.setAttribute('aria-disabled', String(disabled));
    }
  }

  _updatePlaceholder() {
    const empty = (this._editor.innerText || '').length === 0;
    this._placeholder.style.display = empty && this._placeholder.textContent ? 'block' : 'none';
  }

  _onInput = (e) => {
    const v = this._editor ? this._editor.innerText : '';
    if (this.getAttribute('value') !== v) this.setAttribute('value', v);
    this.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true }));
    this._updatePlaceholder();
  };

  _onBlur = (e) => {
    const v = this._editor ? this._editor.innerText : '';
    if (v !== this._lastValue) {
      this._lastValue = v;
      this.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
    }
  };

  _onKeyDown = (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      this._editor.blur(); // Commit the value
    } else if (e.key === 'Escape') {
      e.preventDefault();
      // Restore to last committed value
      this._editor.innerText = this._lastValue;
      this.setAttribute('value', this._lastValue);
      this._updatePlaceholder();
      this._editor.blur();
    }
  };

  _onPaste = (e) => {
    e.preventDefault();
    // Get plain text only, strip any HTML formatting
    const text = (e.clipboardData || window.clipboardData).getData('text/plain');
    
    // Use modern Selection API instead of deprecated execCommand
    const selection = window.getSelection();
    if (selection.rangeCount > 0) {
      const range = selection.getRangeAt(0);
      range.deleteContents();
      range.insertNode(document.createTextNode(text));
      range.collapse(false);
    }
  };

  // Public method to focus the input
  focus() {
    this._editor.focus();
  }

  blur() {
    this._editor.blur();
  }

  // JS property getter/setter for .value
  get value() {
    return this._editor ? this._editor.innerText : '';
  }

  set value(val) {
    if (val == null) val = '';
    this.setAttribute('value', String(val));
  }
}

customElements.define('x-text', XText);
