// x-button.js
import { XInputBase } from './x-input-base.js';

class XButton extends XInputBase {
  constructor() {
    super();
  }

  static get observedAttributes() {
    return ['disabled', 'text'];
  }

  _createStructure(shadow) {
    // wrapper for positioning
    this._wrapper = document.createElement('div');
    this._wrapper.className = 'wrapper';

    // button element
    this._editor = document.createElement('div');
    this._editor.setAttribute('role', 'button');
    this._editor.tabIndex = 0;
    this._editor.className = 'button-element';
    this._editor.setAttribute('aria-label', 'Button');

    this._wrapper.append(this._editor);
    shadow.append(this._wrapper);
  }

  _getCustomStyles() {
    return `
      :host { 
        display: inline-block; 
        font: inherit; 
        vertical-align: top;
      }
      .wrapper { 
        display: inline-block;
      }
      .button-element {
        min-width: 4ch;
        min-height: 1.5em;
        padding: 0.5em 1em;
        border: 1px solid #ccc;
        border-radius: 4px;
        outline: none;
        box-sizing: border-box;
        font: inherit;
        line-height: 1.2;
        text-align: center;
        cursor: pointer;
        user-select: none;
        background: white;
        color: inherit;
        display: inline-flex;
        align-items: center;
        justify-content: center;
      }
      .button-element:focus {
        box-shadow: 0 0 0 3px rgba(100,150,250,0.12);
      }
      .button-element:hover:not([aria-disabled="true"]) {
        background: #f5f5f5;
        border-color: #999;
      }
      .button-element:active:not([aria-disabled="true"]) {
        background: #e0e0e0;
        border-color: #666;
      }
      .button-element[aria-disabled="true"] {
        opacity: 0.5;
        cursor: not-allowed;
        color: #999;
      }
    `;
  }

  connectedCallback() {
    // Call parent's connectedCallback first
    if (this._addEventListeners) {
      this._addEventListeners();
    }
    
    // Initialize from attributes
    this._getObservedAttributes().forEach(attr => this._syncFromAttribute(attr));
    
    // Initialize text display
    this._updateText();
    
    console.log('Button connected with text:', this.getAttribute('text'));
  }

  _addEventListeners() {
    this._editor.addEventListener('click', this._onClick);
    this._editor.addEventListener('keydown', this._onKeyDown);
  }

  _removeEventListeners() {
    this._editor.removeEventListener('click', this._onClick);
    this._editor.removeEventListener('keydown', this._onKeyDown);
  }

  // Override this method from base class
  _getObservedAttributes() {
    return this.constructor.observedAttributes;
  }

  _syncFromAttribute(name) {
    if (name === 'text') {
      this._updateText();
      return;
    }
    super._syncFromAttribute(name);
  }

  _updateText() {
    const text = this.getAttribute('text') || 'Button';
    this._editor.textContent = text;
    console.log('Updating button text to:', text);
  }

  _setEditorValue(value) {
    this.setAttribute('text', value);
  }

  _hasFocus() {
    return this._editor.matches(':focus');
  }

  _setDisabled(disabled) {
    this._editor.tabIndex = disabled ? -1 : 0;
    this._editor.setAttribute('aria-disabled', String(disabled));
  }

  _onClick = (e) => {
    if (this._editor.getAttribute('aria-disabled') === 'true') return;
    
    this._fireClickEvent();
  };

  _onKeyDown = (e) => {
    if (this._editor.getAttribute('aria-disabled') === 'true') return;
    
    // Handle Space (primary) and Enter (secondary) keys for button activation
    if (e.key === ' ' || e.key === 'Enter') {
      e.preventDefault();
      this._fireClickEvent();
    }
  };

  _fireClickEvent() {
    // Dispatch a click event that bubbles up for React to catch
    this.dispatchEvent(new MouseEvent('click', { 
      bubbles: true, 
      composed: true,
      cancelable: true 
    }));
  }

  focus() {
    this._editor.focus();
  }

  blur() {
    this._editor.blur();
  }

  // Override these methods since buttons don't have "value" in the traditional sense
  get value() {
    return this.getAttribute('text') || '';
  }

  set value(val) {
    this.setAttribute('text', String(val || ''));
  }
}

customElements.define('x-button', XButton);