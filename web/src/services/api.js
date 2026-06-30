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

/** GET /catalog/episodes/{id} */
export async function fetchEpisode(id) {
  const r = await fetch(`${BASE}/catalog/episodes/${id}`)
  if (!r.ok) throw new Error(`Episódio ${id} não encontrado (${r.status})`)
  return r.json()
}

/**
 * Publica EpisodeCompletedEvent no Kafka.
 * POST /episodes/{episodeId}/completed?userId=...&sessionId=...
 * Retorna { userId, sessionId, episodeId, nextEpisodeId, message }
 */
export async function completeEpisode(episodeId, userId, sessionId) {
  const r = await fetch(
    `${BASE}/episodes/${episodeId}/completed?userId=${userId}&sessionId=${sessionId}`,
    { method: 'POST', headers: { 'Content-Type': 'application/json' } }
  )
  if (!r.ok) throw new Error(`Erro ao completar episódio ${episodeId} (${r.status})`)
  return r.json()
}

/**
 * Consulta o estado de reprodução pré-computado pelo Consumer Kafka.
 * GET /episodes/{episodeId}/playback-state?userId=...&sessionId=...
 * Retorna { episodeId, startAtSeconds, videoStorageKey, sessionId, source } ou null (404).
 */
export async function fetchPlaybackState(episodeId, userId, sessionId) {
  const r = await fetch(
    `${BASE}/episodes/${episodeId}/playback-state?userId=${userId}&sessionId=${sessionId}`
  )
  if (r.status === 404) return null
  if (!r.ok) throw new Error(`Erro ao buscar playback-state (${r.status})`)
  return r.json()
}

/**
 * Notifica o backend que o player iniciou a reprodução.
 * POST /episodes/{episodeId}/playback-started
 * Publica PlaybackStartedEvent → Consumer marca PlaybackState como consumido.
 * Impede que o mesmo skip seja aplicado numa segunda abertura.
 */
export async function notifyPlaybackStarted(episodeId, userId, startAtSeconds, sessionId) {
  const r = await fetch(`${BASE}/episodes/${episodeId}/playback-started`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId, startAtSeconds, sessionId })
  })
  if (!r.ok) throw new Error(`Erro ao notificar playback-started (${r.status})`)
  return r.json()
}

/**
 * Heartbeat de posição enviado a cada 30s enquanto o usuário assiste.
 * POST /episodes/{episodeId}/position
 * Publica PlaybackPositionEvent → Consumer grava WatchProgress ("continue watching").
 */
export async function reportPosition(episodeId, userId, positionSeconds, sessionId) {
  const r = await fetch(`${BASE}/episodes/${episodeId}/position`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userId, positionSeconds, sessionId })
  })
  if (!r.ok) throw new Error(`Erro ao reportar posição (${r.status})`)
  return r.json()
}
