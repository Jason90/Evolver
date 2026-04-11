window.evolverAuth = {
  getToken: () => localStorage.getItem('evolver_token'),
  setToken: (t) => localStorage.setItem('evolver_token', t),
  clearToken: () => localStorage.removeItem('evolver_token')
};

// 便于 Blazor IJSRuntime 以单函数名调用
window.evolverAuthGetToken = () => window.evolverAuth.getToken();
window.evolverAuthSetToken = (t) => window.evolverAuth.setToken(t);
window.evolverAuthClearToken = () => window.evolverAuth.clearToken();
