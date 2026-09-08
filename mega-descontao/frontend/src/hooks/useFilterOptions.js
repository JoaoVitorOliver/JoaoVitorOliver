import { useEffect, useState } from 'react'
import { fetchCategories, fetchStores } from '../api'

export function useFilterOptions() {
  const [options, setOptions] = useState({ stores: [], categories: [] })

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([fetchStores(controller.signal), fetchCategories(controller.signal)])
      .then(([stores, categories]) => setOptions({ stores, categories }))
      // Os filtros são um acessório: se falharem, a vitrine continua listando produtos.
      .catch(() => {})

    return () => controller.abort()
  }, [])

  return options
}
