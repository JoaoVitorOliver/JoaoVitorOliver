import { useCallback, useEffect, useState } from 'react'
import { fetchProducts } from '../api'

const EMPTY_RESULT = { items: [], page: 1, pageSize: 12, total: 0, totalPages: 0 }

export function useProducts({ search, store, category, sort, page, pageSize }) {
  const [result, setResult] = useState(EMPTY_RESULT)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    setLoading(true)
    setError(null)

    fetchProducts({ search, store, category, sort, page, pageSize }, controller.signal)
      .then((data) => setResult(data))
      .catch((err) => {
        if (err.name !== 'AbortError') {
          setError(err)
          setResult(EMPTY_RESULT)
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      })

    return () => controller.abort()
  }, [search, store, category, sort, page, pageSize, reloadToken])

  const reload = useCallback(() => setReloadToken((token) => token + 1), [])

  return { ...result, loading, error, reload }
}
