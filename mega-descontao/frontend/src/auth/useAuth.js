import { useCallback, useEffect, useState } from 'react'
import * as authApi from './authApi'

export function useAuth() {
  const [user, setUser] = useState(null)
  const [checking, setChecking] = useState(true)

  const refresh = useCallback(async () => {
    setUser(await authApi.fetchCurrentUser())
    setChecking(false)
  }, [])

  useEffect(() => {
    // Visitante anônimo é o caso normal: /me responde 401 e seguimos sem sessão.
    refresh()
  }, [refresh])

  const entrar = useCallback(
    async (email, senha) => {
      await authApi.login(email, senha)
      await refresh()
    },
    [refresh],
  )

  const cadastrar = useCallback(
    async (email, senha) => {
      await authApi.register(email, senha)
      // Cadastrou, já entra: pedir para digitar de novo o que acabou de digitar é atrito à toa.
      await authApi.login(email, senha)
      await refresh()
    },
    [refresh],
  )

  const sair = useCallback(async () => {
    await authApi.logout()
    setUser(null)
  }, [])

  return { user, checking, entrar, cadastrar, sair, refresh }
}
