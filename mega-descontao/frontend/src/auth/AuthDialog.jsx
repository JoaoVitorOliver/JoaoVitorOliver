import { useEffect, useRef, useState } from 'react'

export default function AuthDialog({ open, onClose, onLogin, onRegister }) {
  const [modo, setModo] = useState('entrar')
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState(null)
  const [enviando, setEnviando] = useState(false)
  const primeiroCampo = useRef(null)

  useEffect(() => {
    if (open) {
      setErro(null)
      primeiroCampo.current?.focus()
    }
  }, [open, modo])

  useEffect(() => {
    if (!open) return

    const aoTeclar = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', aoTeclar)
    return () => window.removeEventListener('keydown', aoTeclar)
  }, [open, onClose])

  if (!open) {
    return null
  }

  async function enviar(event) {
    event.preventDefault()
    setEnviando(true)
    setErro(null)

    try {
      await (modo === 'entrar' ? onLogin(email, senha) : onRegister(email, senha))
      setEmail('')
      setSenha('')
      onClose()
    } catch (err) {
      setErro(err.message)
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="modal" role="dialog" aria-modal="true" aria-label="Entrar na conta">
      {/* Clique fora fecha: quem abriu sem querer não fica preso. */}
      <button type="button" className="modal__backdrop" aria-label="Fechar" onClick={onClose} />

      <div className="modal__card">
        <div className="modal__tabs" role="tablist">
          <button
            type="button"
            role="tab"
            aria-selected={modo === 'entrar'}
            className={`modal__tab ${modo === 'entrar' ? 'modal__tab--active' : ''}`}
            onClick={() => setModo('entrar')}
          >
            Entrar
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={modo === 'cadastrar'}
            className={`modal__tab ${modo === 'cadastrar' ? 'modal__tab--active' : ''}`}
            onClick={() => setModo('cadastrar')}
          >
            Criar conta
          </button>
        </div>

        <form className="modal__form" onSubmit={enviar}>
          <p className="modal__hint">
            {modo === 'entrar'
              ? 'Entre para montar um feed com os nichos que te interessam.'
              : 'Crie sua conta para receber um feed com a sua cara. Leva menos de um minuto.'}
          </p>

          <label className="field">
            <span className="field__label">E-mail</span>
            <input
              ref={primeiroCampo}
              type="email"
              className="search__input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
              required
            />
          </label>

          <label className="field">
            <span className="field__label">Senha</span>
            <input
              type="password"
              className="search__input"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              autoComplete={modo === 'entrar' ? 'current-password' : 'new-password'}
              minLength={8}
              required
            />
            {modo === 'cadastrar' && (
              <small className="modal__rule">Mínimo de 8 caracteres, com maiúscula, minúscula e número.</small>
            )}
          </label>

          {erro && <p className="admin__error">{erro}</p>}

          <button type="submit" className="cta" disabled={enviando}>
            {enviando ? 'Aguarde...' : modo === 'entrar' ? 'Entrar' : 'Criar conta'}
          </button>

          <button type="button" className="link-button" onClick={onClose}>
            Continuar sem conta
          </button>
        </form>
      </div>
    </div>
  )
}
