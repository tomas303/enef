// x-number.js
import { XInputBase } from './x-input-base.js';

class XNumber extends XInputBase {
  constructor() {
    super();
  }

  static get observedAttributes() {
    return ['value', 'disabled', 'placeholder', 'decimal-places'];
  }

  _createStructure(shadow) {
    // wrapper for positioning placeholder
    this._wrapper = document.createElement('div');
    this._wrapper.className = 'wrapper';

    // Container for the number input parts
    this._numberContainer = document.createElement('div');
    this._numberContainer.className = 'number-container';

    // Integer part (before decimal)
    this._integerPart = document.createElement('div');
    this._integerPart.setAttribute('role', 'textbox');
    this._integerPart.tabIndex = 0;
    this._integerPart.contentEditable = 'true';
    this._integerPart.className = 'editable integer-part';

    // Decimal separator (fixed dot)
    this._decimalSeparator = document.createElement('span');
    this._decimalSeparator.className = 'decimal-separator';
    this._decimalSeparator.textContent = '.';

    // Decimal part (after decimal)
    this._decimalPart = document.createElement('div');
    this._decimalPart.setAttribute('role', 'textbox');
    this._decimalPart.tabIndex = -1; // Not directly tabbable
    this._decimalPart.contentEditable = 'true';
    this._decimalPart.className = 'editable decimal-part';

    // placeholder element
    this._placeholder = document.createElement('div');
    this._placeholder.className = 'placeholder';
    this._placeholder.setAttribute('aria-hidden', 'true');

    this._numberContainer.append(this._integerPart, this._decimalSeparator, this._decimalPart);
    this._wrapper.append(this._placeholder, this._numberContainer);
    shadow.append(this._wrapper);

    // Set the main editor reference to integer part for base class compatibility
    this._editor = this._integerPart;
  }

  _getCustomStyles() {
    return `
      .number-container {
        display: flex;
        align-items: baseline;
        min-width: 6ch;
        min-height: 1.5em;
        padding: 0.5em;
        border: 1px solid #ccc;
        border-radius: 4px;
        outline: none;
        box-sizing: border-box;
        font: inherit;
        line-height: 1.2;
        background: white;
      }
      .integer-part {
        text-align: right;
        flex: 1;
        border: none;
        outline: none;
        background: transparent;
        padding: 0;
        margin: 0;
        font: inherit;
        line-height: inherit;
      }
      .decimal-separator {
        color: #666;
        user-select: none;
        font: inherit;
        line-height: inherit;
      }
      .decimal-part {
        text-align: left;
        flex: 0 0 auto;
        border: none;
        outline: none;
        background: transparent;
        padding: 0;
        margin: 0;
        font: inherit;
        line-height: inherit;
      }
      .decimal-part:empty::before {
        content: "\\00a0";
        color: transparent;
      }
      .decimal-part.hidden {
        display: none;
      }
      .decimal-separator.hidden {
        display: none;
      }
      .number-container:focus-within {
        box-shadow: 0 0 0 3px rgba(100,150,250,0.12);
      }
      /* Override base editable styles */
      .integer-part.editable,
      .decimal-part.editable {
        border: none;
        padding: 0;
        min-height: auto;
        border-radius: 0;
      }
    `;
  }

  _addEventListeners() {
    // Add event listeners for both integer and decimal parts
    this._integerPart.addEventListener('input', this._onInput);
    this._integerPart.addEventListener('blur', this._onBlur);
    this._integerPart.addEventListener('keydown', this._onIntegerKeyDown);
    this._integerPart.addEventListener('paste', this._onPaste);
    
    this._decimalPart.addEventListener('input', this._onDecimalInput);
    this._decimalPart.addEventListener('keydown', this._onDecimalKeyDown);
  }

  _removeEventListeners() {
    this._integerPart.removeEventListener('input', this._onInput);
    this._integerPart.removeEventListener('blur', this._onBlur);
    this._integerPart.removeEventListener('keydown', this._onIntegerKeyDown);
    this._integerPart.removeEventListener('paste', this._onPaste);
    
    this._decimalPart.removeEventListener('input', this._onDecimalInput);
    this._decimalPart.removeEventListener('keydown', this._onDecimalKeyDown);
  }

  _hasFocus() {
    return this._integerPart.matches(':focus') || this._decimalPart.matches(':focus');
  }

  _setDisabled(disabled) {
    this._integerPart.contentEditable = disabled ? 'false' : 'true';
    this._integerPart.tabIndex = disabled ? -1 : 0;
    this._integerPart.setAttribute('aria-disabled', String(disabled));
    
    this._decimalPart.contentEditable = disabled ? 'false' : 'true';
    this._decimalPart.setAttribute('aria-disabled', String(disabled));
  }

  connectedCallback() {
    super.connectedCallback();
    // Update decimal places visibility after connection
    this._updateDecimalVisibility();
  }

  _syncFromAttribute(name) {
    if (name === 'decimal-places') {
      this._updateDecimalVisibility();
      return;
    }
    super._syncFromAttribute(name);
  }

  _updateDecimalVisibility() {
    const decimalPlaces = parseInt(this.getAttribute('decimal-places') || '0');
    if (decimalPlaces > 0) {
      this._decimalSeparator.classList.remove('hidden');
      this._decimalPart.classList.remove('hidden');
      // Set width based on decimal places, with minimum for cursor visibility
      this._decimalPart.style.width = `${Math.max(decimalPlaces, 1)}ch`;
    } else {
      this._decimalSeparator.classList.add('hidden');
      this._decimalPart.classList.add('hidden');
    }
  }

  _getEditorValue() {
    const integerText = this._integerPart.innerText || '';
    const decimalPlaces = parseInt(this.getAttribute('decimal-places') || '0');
    
    if (decimalPlaces > 0) {
      const decimalText = this._decimalPart.innerText || '';
      return decimalText ? `${integerText}.${decimalText}` : integerText;
    }
    return integerText;
  }

  _setEditorValue(value) {
    const parts = String(value).split('.');
    const integerPart = parts[0] || '';
    const decimalPart = parts[1] || '';
    
    this._integerPart.innerText = integerPart;
    
    const decimalPlaces = parseInt(this.getAttribute('decimal-places') || '0');
    if (decimalPlaces > 0) {
      this._decimalPart.innerText = decimalPart.substring(0, decimalPlaces);
    }
  }

  _validateInput(value) {
    // Remove any non-numeric characters except minus and decimal point
    let cleaned = value.replace(/[^-0-9.]/g, '');
    
    // Handle negative numbers - only allow minus at the beginning
    const hasNegative = cleaned.startsWith('-');
    cleaned = cleaned.replace(/-/g, '');
    if (hasNegative) {
      cleaned = '-' + cleaned;
    }
    
    // Handle decimal point
    const decimalPlaces = parseInt(this.getAttribute('decimal-places') || '0');
    if (decimalPlaces > 0) {
      const parts = cleaned.split('.');
      if (parts.length > 2) {
        // Multiple decimal points - keep only the first
        cleaned = parts[0] + '.' + parts.slice(1).join('');
      }
      
      // Limit decimal places
      const [integer, decimal] = cleaned.split('.');
      if (decimal && decimal.length > decimalPlaces) {
        cleaned = integer + '.' + decimal.substring(0, decimalPlaces);
      }
    } else {
      // No decimal places allowed
      cleaned = cleaned.replace(/\./g, '');
    }
    
    return cleaned;
  }

  _onDecimalInput = (e) => {
    const decimalText = this._decimalPart.innerText;
    const decimalPlaces = parseInt(this.getAttribute('decimal-places') || '0');
    
    // Validate and limit decimal part
    const validatedDecimal = decimalText.replace(/[^0-9]/g, '').substring(0, decimalPlaces);
    if (validatedDecimal !== decimalText) {
      this._decimalPart.innerText = validatedDecimal;
    }
    
    // Update the main component value and trigger events manually
    const fullValue = this._getEditorValue();
    if (this.getAttribute('value') !== fullValue) {
      this.setAttribute('value', fullValue);
    }
    
    this.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true }));
    this._updatePlaceholder();
  };

  _onIntegerKeyDown = (e) => {
    // Allow navigation to decimal part with arrow keys
    if (e.key === 'ArrowRight' && this._getCaretPosition(this._integerPart) === this._integerPart.innerText.length) {
      const decimalPlaces = parseInt(this.getAttribute('decimal-places') || '0');
      if (decimalPlaces > 0) {
        e.preventDefault();
        this._decimalPart.focus();
        this._setCaretPosition(this._decimalPart, 0);
      }
    }
    
    this._onKeyDown(e);
  };

  _onDecimalKeyDown = (e) => {
    // Allow navigation back to integer part
    if (e.key === 'ArrowLeft' && this._getCaretPosition(this._decimalPart) === 0) {
      e.preventDefault();
      this._integerPart.focus();
      this._setCaretPosition(this._integerPart, this._integerPart.innerText.length);
    }
    
    this._onKeyDown(e);
  };

  _getCaretPosition(element) {
    const selection = window.getSelection();
    if (selection.rangeCount > 0) {
      const range = selection.getRangeAt(0);
      if (element.contains(range.startContainer)) {
        return range.startOffset;
      }
    }
    return 0;
  }

  _setCaretPosition(element, position) {
    const range = document.createRange();
    const selection = window.getSelection();
    
    if (element.firstChild) {
      range.setStart(element.firstChild, Math.min(position, element.innerText.length));
    } else {
      range.setStart(element, 0);
    }
    range.collapse(true);
    
    selection.removeAllRanges();
    selection.addRange(range);
  }

  focus() {
    this._integerPart.focus();
  }
}

customElements.define('x-number', XNumber);