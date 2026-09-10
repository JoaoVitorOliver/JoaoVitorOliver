import { useEffect, useRef, useState } from 'react'

export default function Header({ search, onSearchChange, user, onEntrar, onSair }) {
  const [menuAberto, setMenuAberto] = useState(false)
  const menu = useRef(null)

  useEffect(() => {
    if (!menuAberto) return

    const aoClicarFora = (e) => {
      if (menu.current && !menu.current.contains(e.target)) {
        setMenuAberto(false)
      }
    }

    document.addEventListener('mousedown', aoClicarFora)
    return () => document.removeEventListener('mousedown', aoClicarFora)
  }, [menuAberto])

  return (
    <header className="header">
      <div className="container header__inner">
        <a className="brand" href="#/">
          <span className="brand__mark" aria-hidden="true">
            %
          </span>
          <span className="brand__text">
            <strong>Mega Descontão</strong>
            <small>as melhores promoções dos maiores marketplaces</small>
          </span>
        </a>

        <div className="search">
          <span className="search__icon" aria-hidden="true">
            ⌕
          </span>
          <input
            type="search"
            className="search__input"
            placeholder="O que você está procurando?"
            aria-label="Buscar ofertas"
            value={search}
            onChange={(event) => onSearchChange(event.target.value)}
          />
        </div>

        {/* Conta é opcional: quem não quer entrar continua usando o site inteiro. */}
        {user ? (
          <div className="account" ref={menu}>
            <button
              type="button"
              className="account__button"
              aria-expanded={menuAberto}
              onClick={() => setMenuAberto((aberto) => !aberto)}
            >
              <span className="account__avatar" aria-hidden="true">
                {user.email.slice(0, 1).toUpperCase()}
              </span>
              <span className="account__email">{user.email}</span>
            </button>

            {menuAberto && (
              <div className="account__menu">
                {user.isAdmin && (
                  <a className="account__item" href="#/admin">
                    Curadoria
                  </a>
                )}
                <button type="button" className="account__item" onClick={onSair}>
                  Sair
                </button>
              </div>
            )}
          </div>
        ) : (
          <button type="button" className="account__enter" onClick={onEntrar}>
            Entrar
          </button>
        )}
      </div>
    </header>
  )
}
