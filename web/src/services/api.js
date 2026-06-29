/**
 * api.js — camada de comunicação com o backend (.NET API)
 *
 * Em desenvolvimento (Vite dev server): as chamadas a /api/* são
 * reescritas para http://localhost:5000/* pelo proxy do vite.config.js.
 *
 * Em produção (Docker): o nginx do container web faz o proxy de
 * /api/* → http://netflix-api:8080/* via rede interna Docker.
 *
 * O browser nunca faz chamadas cross-origin: tudo passa pelo mesmo origin.
 */

const BASE = '/api'

/**
 * Busca os dados de um episódio do catálogo.
 * GET /catalog/episodes/{id}
 */
export async function fetchEpisode(id) {
  const r = await fetch(`${BASE}/catalog/episodes/${id}`)
  if (!r.ok) throw new Error(`Episódio ${id} não encontrado (${r.status})`)
  return r.json()
}

/**
 * Publica o EpisodeCompletedEvent no Kafka.
 * POST /episodes/{episodeId}/completed
 * Retorna { userId, episodeId, nextEpisodeId, message }
 */
export async function completeEpisode(episodeId) {
  const r = await fetch(`${BASE}/episodes/${episodeId}/completed`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' }
  })
  if (!r.ok) throw new Error(`Erro ao completar episódio ${episodeId} (${r.status})`)
  return r.json()
}

/**
 * Consulta o estado de reprodução pré-computado pelo Consumer Kafka.
 * GET /episodes/{episodeId}/playback-state?userId={userId}
 * Retorna { episodeId, startAtSeconds, videoStorageKey, source } ou null (404).
 */
export async function fetchPlaybackState(episodeId, userId) {
  const r = await fetch(`${BASE}/episodes/${episodeId}/playback-state?userId=${userId}`)
  if (r.status === 404) return null
  if (!r.ok) throw new Error(`Erro ao buscar playback-state (${r.status})`)
  return r.json()
}
