import { API_URL } from '../api'

// credentials: 'include' em tudo — a sessão vive num cookie httpOnly, que o JavaScript não
// lê nem pode vazar. Sem isso o cookie não viaja em desenvolvimento, onde o front está na
// 5173 e a API na 5080.
async function post(path, body) {
  const response = await fetch(`${API_URL}${path}`, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: body ? JSON.stringify(body) : undefined,
  })

  if (response.ok) {
    return response.status === 204 ? null : response.text().then((t) => (t ? JSON.parse(t) : null))
  }

  throw new Error(await describeError(response))
}

async function describeError(response) {
  if (response.status === 401) {
    return 'E-mail ou senha incorretos.'
  }

  try {
    const problem = await response.json()

    if (problem.errors) {
      return Object.values(problem.errors).flat().map(traduzir).join(' ')
    }

    return problem.detail ?? problem.title ?? `A API respondeu ${response.status}`
  } catch {
    return `A API respondeu ${response.status}`
  }
}

/// As mensagens do Identity vêm em inglês. Traduzir as comuns evita jogar "Passwords must
/// have at least one uppercase" na cara de quem só quer se cadastrar.
function traduzir(mensagem) {
  const texto = String(mensagem)

  if (/at least one upper/i.test(texto)) return 'A senha precisa de ao menos uma letra maiúscula.'
  if (/at least one lower/i.test(texto)) return 'A senha precisa de ao menos uma letra minúscula.'
  if (/at least one digit/i.test(texto)) return 'A senha precisa de ao menos um número.'
  if (/must be at least/i.test(texto)) return 'A senha precisa ter 8 caracteres ou mais.'
  if (/already taken|DuplicateUserName/i.test(texto)) return 'Já existe uma conta com esse e-mail.'
  if (/is invalid|not valid/i.test(texto)) return 'E-mail inválido.'

  return texto
}

export async function fetchCurrentUser() {
  const response = await fetch(`${API_URL}/api/auth/me`, { credentials: 'include' })
  return response.ok ? response.json() : null
}

export const login = (email, password) => post('/api/auth/login?useCookies=true', { email, password })

export const register = (email, password) => post('/api/auth/register', { email, password })

export const logout = () => post('/api/auth/logout')
