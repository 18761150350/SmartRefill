// Create a new file to add a polyfill for process
if (typeof process === 'undefined') {
  window.process = {
    env: { NODE_ENV: 'development' }
  };
} 