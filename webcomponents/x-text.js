// x-text.js
class XText extends HTMLElement {
  constructor() {
    super();
    // Create a shadow root to encapsulate DOM and styling
    const shadow = this.attachShadow({ mode: 'open' });

    // Create input element
    this.input = document.createElement('input');
    this.input.type = 'text';

    // Basic styling (optional)
    const style = document.createElement('style');
    style.textContent = `
      input {
        font: inherit;
        padding: 0.25em;
        border: 1px solid #ccc;
        border-radius: 4px;
      }
    `;

    shadow.append(style, this.input);
  }

  connectedCallback() {
    // Initialize value from attribute
    this.input.value = this.getAttribute('value') || '';

    // Emit 'change' event on user input
    this.input.addEventListener('input', () => {
      this.setAttribute('value', this.input.value);
      this.dispatchEvent(new Event('change'));
    });
  }

  static get observedAttributes() {
    return ['value', 'disabled', 'placeholder'];
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (name === 'value' && this.input.value !== newValue) {
      this.input.value = newValue || '';
    }
    if (name === 'disabled') {
      this.input.disabled = newValue !== null;
    }
    if (name === 'placeholder') {
      this.input.placeholder = newValue || '';
    }
  }

  // JS property getter/setter for .value
  get value() {
    return this.input.value;
  }

  set value(val) {
    this.setAttribute('value', val);
  }
}

// Register the custom element
customElements.define('x-text', XText);
