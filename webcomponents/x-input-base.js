// x-input-base.js - Base class for custom input components
export class XInputBase extends HTMLElement {
  constructor() {
    super();
    const shadow = this.attachShadow({ mode: 'open' });

    // Create component-specific structure (override in subclasses)
    this._createStructure(shadow);
    this._createStyles(shadow);

    // track last dispatched value for change detection
    this._lastValue = '';
  }

  // Override this in subclasses to create component-specific structure
  _createStructure(shadow) {
    // Base implementation - subclasses should override this
    throw new Error('_createStructure must be implemented by subclass');
  }

  _createStyles(shadow) {
    const style = document.createElement('style');
    style.textContent = this._getBaseStyles() + this._getCustomStyles();
    shadow.append(style);
  }

  _getBaseStyles() {
    return `
      :host { 
        display: inline-block; 
        font: inherit; 
        position: relative;
        vertical-align: top;
      }
      .wrapper { 
        position: relative; 
      }
      .editable {
        min-width: 6ch;
        min-height: 1.5em;
        padding: 0.5em;
        border: 1px solid #ccc;
        border-radius: 4px;
        outline: none;
        box-sizing: border-box;
        font: inherit;
        line-height: 1.2;
      }
      .editable:focus { 
        box-shadow: 0 0 0 3px rgba(100,150,250,0.12); 
      }
      .placeholder {
        position: absolute;
        left: 0.5em;
        top: 0.5em;
        color: #888;
        pointer-events: none;
        user-select: none;
        font: inherit;
        line-height: 1.2;
      }
    `;
  }

  // Override this in subclasses for component-specific styles
  _getCustomStyles() {
    return '';
  }

  connectedCallback() {
    // Initialize from attributes
    this._getObservedAttributes().forEach(attr => this._syncFromAttribute(attr));

    // Add event listeners - let subclasses handle this
    this._addEventListeners();
  }

  disconnectedCallback() {
    // Remove event listeners - let subclasses handle this
    this._removeEventListeners();
  }

  // Override these in subclasses to add component-specific event listeners
  _addEventListeners() {
    // Base implementation - subclasses should override this
  }

  _removeEventListeners() {
    // Base implementation - subclasses should override this
  }

  static get observedAttributes() {
    return ['value', 'disabled', 'placeholder'];
  }

  // Override this in subclasses to add more attributes
  _getObservedAttributes() {
    return this.constructor.observedAttributes;
  }

  attributeChangedCallback(name, oldValue, newValue) {
    this._syncFromAttribute(name);
  }

  // internal: keep placeholder/editor/disabled in sync
  _syncFromAttribute(name) {
    if (name === 'value') {
      const v = this.getAttribute('value') || '';
      if (this._getEditorValue() !== v) {
        this._setEditorValue(v);
        // Only update _lastValue when syncing from external source (not user input)
        if (!this._hasFocus()) {
          this._lastValue = v;
        }
      }
      this._updatePlaceholder();
    }
    if (name === 'placeholder') {
      this._placeholder.textContent = this.getAttribute('placeholder') || '';
      this._updatePlaceholder();
    }
    if (name === 'disabled') {
      const disabled = this.hasAttribute('disabled');
      this._setDisabled(disabled);
    }
  }

  _updatePlaceholder() {
    const empty = (this._getEditorValue() || '').length === 0;
    this._placeholder.style.display = empty && this._placeholder.textContent ? 'block' : 'none';
  }

  // Override these in subclasses for different input handling
  _getEditorValue() {
    throw new Error('_getEditorValue must be implemented by subclass');
  }

  _setEditorValue(value) {
    throw new Error('_setEditorValue must be implemented by subclass');
  }

  _hasFocus() {
    throw new Error('_hasFocus must be implemented by subclass');
  }

  _setDisabled(disabled) {
    throw new Error('_setDisabled must be implemented by subclass');
  }

  // Override this in subclasses for input validation
  _validateInput(value) {
    return value; // Base class accepts any input
  }

  // Base event handlers - subclasses can override these
  _onInput = (e) => {
    const rawValue = this._getEditorValue();
    const validatedValue = this._validateInput(rawValue);
    
    // If validation changed the value, update the editor
    if (validatedValue !== rawValue) {
      this._setEditorValue(validatedValue);
      // Restore cursor position if possible
      this._restoreCursor();
    }

    if (this.getAttribute('value') !== validatedValue) {
      this.setAttribute('value', validatedValue);
    }
    
    this.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true }));
    this._updatePlaceholder();
  };

  _onBlur = (e) => {
    const v = this._getEditorValue();
    if (v !== this._lastValue) {
      this._lastValue = v;
      this.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
    }
  };

  _onKeyDown = (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      this.blur(); // Commit the value
    } else if (e.key === 'Escape') {
      e.preventDefault();
      // Restore to last committed value
      this._setEditorValue(this._lastValue);
      this.setAttribute('value', this._lastValue);
      this._updatePlaceholder();
      this.blur();
    }
  };

  _onPaste = (e) => {
    e.preventDefault();
    // Get plain text only, strip any HTML formatting
    const text = (e.clipboardData || window.clipboardData).getData('text/plain');
    const validatedText = this._validateInput(text);
    
    // Use modern Selection API instead of deprecated execCommand
    const selection = window.getSelection();
    if (selection.rangeCount > 0) {
      const range = selection.getRangeAt(0);
      range.deleteContents();
      range.insertNode(document.createTextNode(validatedText));
      range.collapse(false);
    }
  };

  _onInput = (e) => {
    const rawValue = this._getEditorValue();
    const validatedValue = this._validateInput(rawValue);
    
    // If validation changed the value, update the editor
    if (validatedValue !== rawValue) {
      this._setEditorValue(validatedValue);
      // Restore cursor position if possible
      this._restoreCursor();
    }

    if (this.getAttribute('value') !== validatedValue) {
      this.setAttribute('value', validatedValue);
    }
    
    this.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true }));
    this._updatePlaceholder();
  };

  _restoreCursor() {
    // Override in subclasses for component-specific cursor restoration
  }

  _onBlur = (e) => {
    const v = this._getEditorValue();
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
      this._setEditorValue(this._lastValue);
      this.setAttribute('value', this._lastValue);
      this._updatePlaceholder();
      this._editor.blur();
    }
  };

  _onPaste = (e) => {
    e.preventDefault();
    // Get plain text only, strip any HTML formatting
    const text = (e.clipboardData || window.clipboardData).getData('text/plain');
    const validatedText = this._validateInput(text);
    
    // Use modern Selection API instead of deprecated execCommand
    const selection = window.getSelection();
    if (selection.rangeCount > 0) {
      const range = selection.getRangeAt(0);
      range.deleteContents();
      range.insertNode(document.createTextNode(validatedText));
      range.collapse(false);
    }
  };

  // Public methods - override these in subclasses
  focus() {
    throw new Error('focus must be implemented by subclass');
  }

  blur() {
    throw new Error('blur must be implemented by subclass');
  }

  // JS property getter/setter for .value
  get value() {
    return this._getEditorValue();
  }

  set value(val) {
    if (val == null) val = '';
    this.setAttribute('value', String(val));
  }
}