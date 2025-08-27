// x-select.js
import { XInputBase } from './x-input-base.js';

class XSelect extends XInputBase {
  constructor() {
    super();
    this._options = [];
    this._filteredOptions = [];
    this._selectedIndex = -1;
    this._isOpen = false;
  }

  static get observedAttributes() {
    return ['value', 'disabled', 'options'];
  }

  _createStructure(shadow) {
    // wrapper for positioning
    this._wrapper = document.createElement('div');
    this._wrapper.className = 'wrapper';

    // Container for input and button
    this._selectContainer = document.createElement('div');
    this._selectContainer.className = 'select-container';

    // Filter input (acts as main editor)
    this._editor = document.createElement('input');
    this._editor.type = 'text';
    this._editor.className = 'filter-input';
    this._editor.setAttribute('role', 'combobox');
    this._editor.setAttribute('aria-expanded', 'false');
    this._editor.setAttribute('aria-autocomplete', 'list');

    // Dropdown button
    this._dropdownButton = document.createElement('button');
    this._dropdownButton.className = 'dropdown-button';
    this._dropdownButton.type = 'button';
    this._dropdownButton.innerHTML = '▼';
    this._dropdownButton.setAttribute('aria-label', 'Open dropdown');
    this._dropdownButton.tabIndex = -1;

    // Dropdown container
    this._dropdown = document.createElement('div');
    this._dropdown.className = 'dropdown hidden';
    this._dropdown.setAttribute('role', 'listbox');

    // Assemble structure
    this._selectContainer.append(this._editor, this._dropdownButton);
    this._wrapper.append(this._selectContainer, this._dropdown);
    shadow.append(this._wrapper);
  }

  _getCustomStyles() {
    return `
      .select-container {
        display: flex;
        align-items: stretch;
        min-width: 6ch;
        border: 1px solid #ccc;
        border-radius: 4px;
        background: white;
        position: relative;
      }
      .select-container:focus-within {
        box-shadow: 0 0 0 3px rgba(100,150,250,0.12);
      }
      .filter-input {
        flex: 1;
        border: none;
        outline: none;
        padding: 0.5em;
        background: transparent;
        font: inherit;
        line-height: 1.2;
        min-height: 1.5em;
        box-sizing: border-box;
      }
      .dropdown-button {
        width: 2ch;
        border: none;
        background: transparent;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #666;
        font-size: 0.8em;
        padding: 0;
        border-left: 1px solid #eee;
      }
      .dropdown-button:hover {
        background: #f5f5f5;
      }
      .dropdown-button:active {
        background: #e0e0e0;
      }
      .dropdown {
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        background: white;
        border: 1px solid #ccc;
        border-top: none;
        border-radius: 0 0 4px 4px;
        max-height: 200px;
        overflow-y: auto;
        z-index: 1000;
        box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      }
      .dropdown.hidden {
        display: none;
      }
      .option {
        padding: 0.5em;
        cursor: pointer;
        border-bottom: 1px solid #f0f0f0;
      }
      .option:last-child {
        border-bottom: none;
      }
      .option:hover {
        background: #f5f5f5;
      }
      .option.selected {
        background: #e8f4f8 !important;
        color: #006080 !important;
      }
      .option.highlighted {
        background: #2196f3 !important;
        color: white !important;
      }
      /* Override base styles */
      .wrapper {
        position: relative;
      }
    `;
  }

  _addEventListeners() {
    this._editor.addEventListener('input', this._onFilterInput);
    this._editor.addEventListener('focus', this._onFocus);
    this._editor.addEventListener('blur', this._onBlur);
    this._editor.addEventListener('keydown', this._onKeyDown);
    this._dropdownButton.addEventListener('click', this._onDropdownClick);
    
    // Close dropdown when clicking outside
    document.addEventListener('click', this._onDocumentClick);
  }

  _removeEventListeners() {
    this._editor.removeEventListener('input', this._onFilterInput);
    this._editor.removeEventListener('focus', this._onFocus);
    this._editor.removeEventListener('blur', this._onBlur);
    this._editor.removeEventListener('keydown', this._onKeyDown);
    this._dropdownButton.removeEventListener('click', this._onDropdownClick);
    document.removeEventListener('click', this._onDocumentClick);
  }

  _syncFromAttribute(name) {
    if (name === 'options') {
      this._parseOptions();
      this._updateDropdown();
      // Re-sync the current value display after options change
      const currentValue = this.getAttribute('value') || '';
      this._setEditorValue(currentValue);
      return;
    }
    if (name === 'value') {
      // Update dropdown when value changes to reflect new selection
      this._updateDropdown();
    }
    super._syncFromAttribute(name);
  }

  _parseOptions() {
    const optionsAttr = this.getAttribute('options');
    if (optionsAttr) {
      try {
        this._options = JSON.parse(optionsAttr);
      } catch (e) {
        this._options = [];
      }
    } else {
      this._options = [];
    }
    this._filteredOptions = [...this._options];
  }

  _getEditorValue() {
    // Return the selected value, not the filter text
    return this.getAttribute('value') || '';
  }

  _setEditorValue(value) {
    // Find the option with this value and show its text
    const option = this._options.find(opt => opt.value === value);
    if (option) {
      this._editor.value = option.text;
    } else {
      this._editor.value = '';
    }
    
    // Update dropdown to reflect new selection
    this._updateDropdown();
  }

  _hasFocus() {
    return this._editor.matches(':focus');
  }

  _setDisabled(disabled) {
    this._editor.disabled = disabled;
    this._dropdownButton.disabled = disabled;
    this._editor.setAttribute('aria-disabled', String(disabled));
    this._dropdownButton.setAttribute('aria-disabled', String(disabled));
  }

  _onFilterInput = (e) => {
    const filterText = this._editor.value.toLowerCase();
    
    // Only filter if we're actually filtering (not just displaying selected value)
    this._filteredOptions = this._options.filter(option => 
      option.text.toLowerCase().includes(filterText)
    );
    
    // Auto-select first option if only one result
    if (this._filteredOptions.length === 1) {
      this._selectedIndex = 0;
    } else {
      this._selectedIndex = -1;
    }
    
    this._updateDropdown();
    
    // Only show dropdown if it's already open (don't auto-open on typing)
    if (this._isOpen) {
      this._highlightOption();
    }
  };

  _onFocus = (e) => {
    // Don't show dropdown on focus - only on manual trigger
  };

  _onBlur = (e) => {
    // Delay hiding to allow clicking on options
    setTimeout(() => {
      if (!this.contains(document.activeElement)) {
        this._hideDropdown();
      }
    }, 150);
  };

  _onKeyDown = (e) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      if (e.altKey) {
        // Alt+ArrowDown toggles dropdown
        this._toggleDropdown();
      } else {
        // Navigate down in options
        this._navigateOptions(1);
      }
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      this._navigateOptions(-1);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (this._isOpen && this._selectedIndex >= 0) {
        this._selectOption(this._filteredOptions[this._selectedIndex]);
      }
    } else if (e.key === 'Escape') {
      e.preventDefault();
      if (this._isOpen) {
        this._hideDropdown();
      } else {
        // Restore to last committed value
        this._setEditorValue(this._lastValue);
        this.setAttribute('value', this._lastValue);
        this.blur();
      }
    }
  };

  _onDropdownClick = (e) => {
    e.preventDefault();
    this._toggleDropdown();
    this._editor.focus();
  };

  _onDocumentClick = (e) => {
    if (!this.contains(e.target)) {
      this._hideDropdown();
    }
  };

  _onOptionClick = (option) => {
    this._selectOption(option);
  };

  _navigateOptions(direction) {
    if (!this._isOpen) {
      this._showDropdown();
      // Auto-select first option if only one result
      if (this._filteredOptions.length === 1) {
        this._selectedIndex = 0;
        this._highlightOption();
      }
      return;
    }

    this._selectedIndex += direction;
    if (this._selectedIndex < 0) {
      this._selectedIndex = this._filteredOptions.length - 1;
    } else if (this._selectedIndex >= this._filteredOptions.length) {
      this._selectedIndex = 0;
    }
    
    this._highlightOption();
  }

  _highlightOption() {
    const options = this._dropdown.querySelectorAll('.option');
    options.forEach((opt, index) => {
      opt.classList.toggle('highlighted', index === this._selectedIndex);
    });

    // Scroll highlighted option into view
    if (this._selectedIndex >= 0 && options[this._selectedIndex]) {
      options[this._selectedIndex].scrollIntoView({ block: 'nearest' });
    }
  }

  _selectOption(option) {
    const newValue = option.value;
    
    // Update the input display immediately
    this._editor.value = option.text;
    
    // Update the component's value attribute
    if (this.getAttribute('value') !== newValue) {
      this.setAttribute('value', newValue);
    }
    
    // Reset filtered options to show all options next time
    this._filteredOptions = [...this._options];
    
    this._hideDropdown();
    this.dispatchEvent(new InputEvent('input', { bubbles: true, composed: true }));
  }

  _showDropdown() {
    this._isOpen = true;
    this._dropdown.classList.remove('hidden');
    this._editor.setAttribute('aria-expanded', 'true');
    
    // Auto-select first option if only one result
    if (this._filteredOptions.length === 1) {
      this._selectedIndex = 0;
      this._highlightOption();
    }
  }

  _hideDropdown() {
    this._isOpen = false;
    this._dropdown.classList.add('hidden');
    this._editor.setAttribute('aria-expanded', 'false');
    this._selectedIndex = -1;
  }

  _toggleDropdown() {
    if (this._isOpen) {
      this._hideDropdown();
    } else {
      this._showDropdown();
    }
  }

  _updateDropdown() {
    this._dropdown.innerHTML = '';
    const currentValue = this.getAttribute('value') || '';
    
    this._filteredOptions.forEach((option, index) => {
      const optionEl = document.createElement('div');
      optionEl.className = 'option';
      optionEl.textContent = option.text;
      optionEl.setAttribute('role', 'option');
      optionEl.setAttribute('data-value', option.value);
      
      // Mark selected option (current value)
      if (option.value === currentValue) {
        optionEl.classList.add('selected');
      }
      
      optionEl.addEventListener('click', () => this._onOptionClick(option));
      this._dropdown.append(optionEl);
    });

    if (this._filteredOptions.length === 0) {
      const noOptions = document.createElement('div');
      noOptions.className = 'option';
      noOptions.textContent = 'No options found';
      noOptions.style.color = '#999';
      noOptions.style.fontStyle = 'italic';
      this._dropdown.append(noOptions);
    }
  }

  focus() {
    this._editor.focus();
  }

  blur() {
    this._editor.blur();
  }
}

customElements.define('x-select', XSelect);