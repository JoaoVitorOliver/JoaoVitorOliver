import { StrictMode, useEffect, useState } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import AdminPage from './admin/AdminPage'
import './styles.css'

// Rota por hash em vez de react-router: são só duas telas, e assim a vitrine não carrega
// uma dependência de roteamento para servir #/admin.
function Router() {
  const [hash, setHash] = useState(window.location.hash)

  useEffect(() => {
    const onChange = () => setHash(window.location.hash)
    window.addEventListener('hashchange', onChange)
    return () => window.removeEventListener('hashchange', onChange)
  }, [])

  return hash.startsWith('#/admin') ? <AdminPage /> : <App />
}

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <Router />
  </StrictMode>,
)
