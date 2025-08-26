// x-date.js
import { XInputBase } from './x-input-base.js';

class XDate extends XInputBase {
  constructor() {
    super();
    // Initialize arrays to prevent errors
    this._parts = [];
    this._separators = [];
  }

  static get observedAttributes() {
    return ['value', 'disabled', 'format', 'day', 'month', 'year'];
  }

  _createStructure(shadow) {
    // wrapper for positioning
    this._wrapper = document.createElement('div');
    this._wrapper.className = 'wrapper';

    // Container for the date input parts
    this._dateContainer = document.createElement('div');
    this._dateContainer.className = 'date-container';

    this._wrapper.append(this._dateContainer);
    shadow.append(this._wrapper);

    // Create parts after adding to shadow DOM
    this._createDateParts();
  }

  _createDateParts() {
    const format = this.getAttribute('format') || 'dd.mm.yyyy';
    const parts = this._parseFormat(format);
    
    console.log('Creating date parts with format:', format, 'parsed:', parts);
    
    this._parts = [];
    this._separators = [];

    parts.forEach((part, index) => {
      if (part.type === 'separator') {
        const separator = document.createElement('span');
        separator.className = 'date-separator';
        separator.textContent = part.value;
        this._dateContainer.append(separator);
        this._separators.push(separator);
      } else {
        const element = document.createElement('div');
        element.setAttribute('role', 'textbox');
        element.tabIndex = index === 0 ? 0 : -1;
        element.contentEditable = 'true';
        element.className = `editable date-part ${part.type}-part`;
        element.dataset.type = part.type;
        element.dataset.maxLength = part.maxLength;
        
        this._dateContainer.append(element);
        this._parts.push({ element, type: part.type, maxLength: part.maxLength });
        
        if (part.type === 'day') this._dayPart = element;
        if (part.type === 'month') this._monthPart = element;
        if (part.type === 'year') this._yearPart = element;
      }
    });

    // Set the main editor reference to first part for base class compatibility
    this._editor = this._parts[0]?.element;
    
    console.log('Created parts:', this._parts.length, 'day:', !!this._dayPart, 'month:', !!this._monthPart, 'year:', !!this._yearPart);
  }

  _parseFormat(format) {
    const parts = [];
    let current = '';
    
    for (let i = 0; i < format.length; i++) {
      const char = format[i];
      
      if (char === 'd' || char === 'm' || char === 'y') {
        current += char;
      } else {
        // Process accumulated characters
        if (current) {
          if (current.startsWith('d')) {
            parts.push({ type: 'day', maxLength: 2 });
          } else if (current.startsWith('m')) {
            parts.push({ type: 'month', maxLength: 2 });
          } else if (current.startsWith('y')) {
            parts.push({ type: 'year', maxLength: 4 });
          }
          current = '';
        }
        
        // Add separator
        parts.push({ type: 'separator', value: char });
      }
    }
    
    // Process final accumulated characters
    if (current) {
      if (current.startsWith('d')) {
        parts.push({ type: 'day', maxLength: 2 });
      } else if (current.startsWith('m')) {
        parts.push({ type: 'month', maxLength: 2 });
      } else if (current.startsWith('y')) {
        parts.push({ type: 'year', maxLength: 4 });
      }
    }
    
    return parts;
  }

  _getCustomStyles() {
    return `
      /* Override base editable styles for date container */
      .date-container {
        display: flex;
        align-items: baseline;
        min-width: 8ch;
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
      
      /* Reset all base editable styles for date parts */
      .date-part.editable {
        min-width: unset !important;
        min-height: unset !important;
        padding: 0 !important;
        border: none !important;
        border-radius: 0 !important;
        outline: none !important;
        box-sizing: content-box !important;
        background: transparent !important;
        box-shadow: none !important;
        margin: 0;
        font: inherit;
        line-height: inherit;
        text-align: center;
        overflow: hidden;
      }
      
      .date-part.editable:focus {
        box-shadow: none !important;
        background: rgba(100,150,250,0.05) !important;
      }
      
      .day-part.editable {
        width: 2ch;
        flex: 0 0 2ch;
        max-width: 2ch;
      }
      .month-part.editable {
        width: 2ch;
        flex: 0 0 2ch;
        max-width: 2ch;
      }
      .year-part.editable {
        width: 4ch;
        flex: 0 0 4ch;
        max-width: 4ch;
      }
      
      .date-part:empty::before {
        content: "\\00a0";
        color: transparent;
      }
      .date-separator {
        color: #666;
        user-select: none;
        font: inherit;
        line-height: inherit;
        margin: 0;
        flex: 0 0 auto;
      }
      .date-container:focus-within {
        box-shadow: 0 0 0 3px rgba(100,150,250,0.12);
      }
      /* Override base wrapper styles */
      .wrapper {
        display: block;
      }
    `;
  }

  _addEventListeners() {
    this._parts.forEach((part, index) => {
      part.element.addEventListener('input', (e) => this._onPartInput(e, part, index));
      part.element.addEventListener('keydown', (e) => this._onPartKeyDown(e, part, index));
      part.element.addEventListener('blur', this._onBlur);
      part.element.addEventListener('paste', this._onPaste);
    });
  }

  _removeEventListeners() {
    this._parts.forEach((part) => {
      part.element.removeEventListener('input', this._onPartInput);
      part.element.removeEventListener('keydown', this._onPartKeyDown);
      part.element.removeEventListener('blur', this._onBlur);
      part.element.removeEventListener('paste', this._onPaste);
    });
  }

  connectedCallback() {
    // Ensure parts are created and event listeners added
    if (!this._parts || this._parts.length === 0) {
      console.log('Creating date parts in connectedCallback');
      this._createDateParts();
    }
    if (this._parts && this._parts.length > 0) {
      this._addEventListeners();
    }
    
    super.connectedCallback();
    
    this._lastFormat = this.getAttribute('format') || 'dd.mm.yyyy';
    console.log('Date component connected with format:', this._lastFormat);
  }

  _needsRebuild() {
    const currentFormat = this.getAttribute('format') || 'dd.mm.yyyy';
    return this._lastFormat !== currentFormat;
  }

  _rebuildParts() {
    // Clear existing parts
    this._dateContainer.innerHTML = '';
    this._removeEventListeners();
    
    // Recreate parts
    this._createDateParts();
    this._addEventListeners();
    
    // Update from current value
    const value = this.getAttribute('value') || '';
    if (value) {
      this._setEditorValue(value);
    }
    
    this._lastFormat = this.getAttribute('format') || 'dd.mm.yyyy';
  }

  _syncFromAttribute(name) {
    if (name === 'format') {
      if (this._needsRebuild()) {
        this._rebuildParts();
      }
      return;
    }
    if (name === 'day' || name === 'month' || name === 'year') {
      this._updateFromIndividualProps();
      return;
    }
    super._syncFromAttribute(name);
  }

  _updateFromIndividualProps() {
    const day = this.getAttribute('day') || '';
    const month = this.getAttribute('month') || '';
    const year = this.getAttribute('year') || '';
    
    if (this._dayPart && day) this._dayPart.innerText = day.padStart(2, '0');
    if (this._monthPart && month) this._monthPart.innerText = month.padStart(2, '0');
    if (this._yearPart && year) this._yearPart.innerText = year;
    
    this._updateValueFromParts();
  }

  _getEditorValue() {
    if (!this._dayPart || !this._monthPart || !this._yearPart) {
      return '';
    }
    
    const day = (this._dayPart.innerText || '1').padStart(2, '0');
    const month = (this._monthPart.innerText || '1').padStart(2, '0');
    const year = this._yearPart.innerText || '1970';
    
    // Return ISO format YYYY-MM-DD
    return `${year}-${month}-${day}`;
  }

  _setEditorValue(value) {
    if (!value) {
      if (this._parts) {
        this._parts.forEach(part => part.element.innerText = '');
      }
      return;
    }
    
    // Parse ISO date format YYYY-MM-DD
    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})$/);
    if (match) {
      const [, year, month, day] = match;
      if (this._dayPart) {
        this._dayPart.innerText = parseInt(day).toString();
      }
      if (this._monthPart) {
        this._monthPart.innerText = parseInt(month).toString();
      }
      if (this._yearPart) {
        this._yearPart.innerText = year;
      }
    }
  }

  _hasFocus() {
    if (!this._parts) return false;
    return this._parts.some(part => part.element.matches(':focus'));
  }

  _setDisabled(disabled) {
    if (!this._parts) return;
    this._parts.forEach(part => {
      part.element.contentEditable = disabled ? 'false' : 'true';
      part.element.tabIndex = disabled ? -1 : (part === this._parts[0] ? 0 : -1);
      part.element.setAttribute('aria-disabled', String(disabled));
    });
  }

  _validateInput(value) {
    // Validate and limit each part
    return value.replace(/[^0-9]/g, '');
  }

  _validatePartValue(value, type) {
    const num = parseInt(value) || 0;
    
    switch(type) {
      case 'day':
        // Day validation: 1-31, but will be refined based on month/year
        return Math.min(Math.max(num, 1), 31);
      case 'month':
        // Month validation: 1-12
        return Math.min(Math.max(num, 1), 12);
      case 'year':
        // Year validation: reasonable range
        return Math.min(Math.max(num, 1000), 9999);
      default:
        return num;
    }
  }

  _getMaxDayForMonth(month, year) {
    const monthNum = parseInt(month) || 1;
    const yearNum = parseInt(year) || 2000;
    
    // Days in each month
    const daysInMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
    
    // Check for leap year
    const isLeapYear = (yearNum % 4 === 0 && yearNum % 100 !== 0) || (yearNum % 400 === 0);
    
    if (monthNum === 2 && isLeapYear) {
      return 29;
    }
    
    return daysInMonth[monthNum - 1] || 31;
  }

  _onPartInput = (e, part, partIndex) => {
    const text = part.element.innerText;
    const validated = this._validateInput(text);
    
    // Limit to max length
    let limited = validated.substring(0, part.maxLength);
    
    // Apply date-specific validation
    if (limited && parseInt(limited) > 0) {
      const validatedValue = this._validatePartValue(limited, part.type);
      
      // Special handling for day validation based on current month/year
      if (part.type === 'day') {
        const currentMonth = this._monthPart?.innerText || '1';
        const currentYear = this._yearPart?.innerText || '2000';
        const maxDay = this._getMaxDayForMonth(currentMonth, currentYear);
        const dayValue = Math.min(validatedValue, maxDay);
        limited = dayValue.toString().padStart(limited.length, '0');
      } else {
        limited = validatedValue.toString().padStart(limited.length, '0');
      }
    }
    
    if (limited !== text) {
      part.element.innerText = limited;
      this._setCaretPosition(part.element, limited.length);
    }
    
    // Only auto-advance if we're at the end AND the field is full
    // This prevents auto-advance when editing in the middle
    const caretPos = this._getCaretPosition(part.element);
    const shouldAutoAdvance = limited.length === part.maxLength && 
                              caretPos === limited.length && 
                              partIndex < this._parts.length - 1;
    
    if (shouldAutoAdvance) {
      // Small delay to allow the user to see the completed field
      setTimeout(() => {
        this._focusNextPart(partIndex);
      }, 0);
    }
    
    // Re-validate day if month or year changed
    if (part.type === 'month' || part.type === 'year') {
      this._revalidateDay();
    }
    
    this._updateValueFromParts();
    this.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true }));
  };

  _revalidateDay() {
    if (!this._dayPart) return;
    
    const currentDay = parseInt(this._dayPart.innerText) || 1;
    const currentMonth = this._monthPart?.innerText || '1';
    const currentYear = this._yearPart?.innerText || '2000';
    const maxDay = this._getMaxDayForMonth(currentMonth, currentYear);
    
    if (currentDay > maxDay) {
      this._dayPart.innerText = maxDay.toString().padStart(2, '0');
    }
  }

  _updateValueFromParts() {
    const newValue = this._getEditorValue();
    if (this.getAttribute('value') !== newValue) {
      this.setAttribute('value', newValue);
    }
  }

  _onPartKeyDown = (e, part, partIndex) => {
    // Handle navigation between parts
    if (e.key === 'ArrowRight' && this._getCaretPosition(part.element) === part.element.innerText.length) {
      e.preventDefault();
      this._focusNextPart(partIndex);
    } else if (e.key === 'ArrowLeft' && this._getCaretPosition(part.element) === 0) {
      e.preventDefault();
      this._focusPreviousPart(partIndex);
    } 
    // Handle seamless backspace
    else if (e.key === 'Backspace' && this._getCaretPosition(part.element) === 0 && partIndex > 0) {
      e.preventDefault();
      this._focusPreviousPart(partIndex);
      const prevPart = this._parts[partIndex - 1];
      const prevText = prevPart.element.innerText;
      if (prevText.length > 0) {
        prevPart.element.innerText = prevText.slice(0, -1);
        this._setCaretPosition(prevPart.element, prevPart.element.innerText.length);
        this._updateValueFromParts();
      }
    }
    // Handle seamless delete
    else if (e.key === 'Delete' && this._getCaretPosition(part.element) === part.element.innerText.length && partIndex < this._parts.length - 1) {
      e.preventDefault();
      this._focusNextPart(partIndex);
      const nextPart = this._parts[partIndex + 1];
      const nextText = nextPart.element.innerText;
      if (nextText.length > 0) {
        nextPart.element.innerText = nextText.substring(1);
        this._setCaretPosition(nextPart.element, 0);
        this._updateValueFromParts();
      }
    }
    
    this._onKeyDown(e);
  };

  _focusNextPart(currentIndex) {
    if (currentIndex < this._parts.length - 1) {
      const nextPart = this._parts[currentIndex + 1];
      nextPart.element.focus();
      this._setCaretPosition(nextPart.element, 0);
    }
  }

  _focusPreviousPart(currentIndex) {
    if (currentIndex > 0) {
      const prevPart = this._parts[currentIndex - 1];
      prevPart.element.focus();
      this._setCaretPosition(prevPart.element, prevPart.element.innerText.length);
    }
  }

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
    if (this._parts.length > 0) {
      this._parts[0].element.focus();
    }
  }

  blur() {
    this._parts.forEach(part => part.element.blur());
  }
}

customElements.define('x-date', XDate);