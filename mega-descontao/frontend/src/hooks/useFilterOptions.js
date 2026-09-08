import { useEffect, useState } from 'react'
import { fetchFilterOptions } from '../api'

export function useFilterOptions() {
  const [options, setOptions] = useState({ stores: [], categories: [] })

  useEffect(() => {
    const controller = new AbortController()

    fetchFilterOptions(controller.signal)
      .then(setOptions)
      // Os filtros são um acessório: se falharem, a vitrine continua listando ofertas.
      .catch(() => {})

    return () => controller.abort()
  }, [])

  return options
}
