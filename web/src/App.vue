<template>
  <div class="player-shell">

    <!-- ── Cabeçalho ─────────────────────────────────────────────────────── -->
    <header class="nf-header">
      <span class="nf-logo">NETFLIX</span>
      <span class="nf-subtitle">Skip Intro · Mensageria Demo</span>
    </header>

    <!-- ── Área do player (sempre no DOM para o <video> nunca ser destruído) -->
    <div class="player-wrap">

      <!-- Vídeo: NUNCA usa v-if, sempre montado -->
      <video
        ref="videoEl"
        class="video"
        :src="VIDEO_SRC"
        @timeupdate="onTimeUpdate"
        @ended="onVideoEnded"
        @loadedmetadata="onMetadataLoaded"
        controls
      ></video>

      <!-- Overlay: loading inicial (sobre o vídeo) -->
      <div v-show="phase === 'loading'" class="overlay-full">
        <div class="spinner"></div>
        <p class="loading-text">Carregando episódio…</p>
      </div>

      <!-- Overlay: erro -->
      <div v-show="phase === 'error'" class="overlay-full">
        <p class="error-icon">⚠</p>
        <p class="error-text">{{ errorMsg }}</p>
        <button class="btn-primary" @click="loadEpisode(1)">Tentar novamente</button>
      </div>

      <!-- Info do episódio (canto inferior esquerdo) -->
      <div class="overlay-info" v-if="episode">
        <p class="ep-meta">T{{ episode.season }} E{{ episode.number }}</p>
        <h1 class="ep-title">{{ episode.title }}</h1>
      </div>

      <!-- Botão Pular Intro -->
      <transition name="fade">
        <button v-if="showSkipIntro" class="btn-skip-intro" @click="skipIntro">
          Pular Introdução
        </button>
      </transition>

      <!-- Botão Próximo Episódio -->
      <transition name="fade">
        <button
          v-if="phase === 'playing' && episode && episode.id < 5"
          class="btn-next"
          @click="goNext"
        >
          ▶ Próximo Episódio
        </button>
      </transition>

      <!-- Fim de série -->
      <div v-if="phase === 'playing' && episode && episode.id >= 5" class="finale-banner">
        Fim da temporada
      </div>

      <!-- Barra de progresso Kafka -->
      <div v-if="phase === 'transitioning'" class="kafka-progress">
        <div class="kafka-bar">
          <div class="kafka-fill" :style="{ width: pollPercent + '%' }"></div>
        </div>
        <p class="kafka-label">{{ kafkaStatus }}</p>
      </div>
    </div>

    <!-- ── Toast ─────────────────────────────────────────────────────────── -->
    <transition name="slide-up">
      <div v-if="notification" class="toast">{{ notification }}</div>
    </transition>

    <!-- ── Console de debug ───────────────────────────────────────────────── -->
    <div class="console-wrap">
      <div class="console-header">
        <span class="console-title">▶ Console de Mensageria</span>
        <span class="console-status">
          video.readyState: <b>{{ readyStateLabel }}</b> |
          currentTime: <b>{{ currentTime.toFixed(1) }}s</b>
        </span>
        <button class="console-clear" @click="logs = []">limpar</button>
      </div>
      <div class="console-body" ref="consoleEl">
        <div
          v-for="(l, i) in logs"
          :key="i"
          class="log-line"
          :class="'log-' + l.level"
        >
          <span class="log-ts">{{ l.ts }}</span>
          <span class="log-level">{{ l.level.toUpperCase().padEnd(7) }}</span>
          <span class="log-msg">{{ l.msg }}</span>
        </div>
        <div v-if="logs.length === 0" class="log-empty">
          aguardando eventos…
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { fetchEpisode, completeEpisode, fetchPlaybackState } from './services/api.js'

// Todos os episódios usam o mesmo arquivo — fonte fixa, nunca muda.
// Isso é fundamental: o <video> NUNCA troca de src, então nunca é reiniciado.
const VIDEO_SRC = '/videos/GTAVI.mp4'

// ── Estado ───────────────────────────────────────────────────────────────────
const episode     = ref(null)
const phase       = ref('loading')   // loading | playing | transitioning | error
const errorMsg    = ref('')
const videoEl     = ref(null)
const consoleEl   = ref(null)
const currentTime = ref(0)
const videoReady  = ref(false)       // true depois de loadedmetadata

const notification = ref('')
const logs         = ref([])

const pendingNext = ref(null)
const pollTimer   = ref(null)
const pollCount   = ref(0)
const kafkaStatus = ref('')
const MAX_POLLS   = 12

// ── Computed ─────────────────────────────────────────────────────────────────
const showSkipIntro = computed(() => {
  if (!episode.value || phase.value !== 'playing') return false
  const { introStartSeconds, introEndSeconds } = episode.value
  return introEndSeconds > 0
    && currentTime.value >= introStartSeconds
    && currentTime.value < introEndSeconds
})

const pollPercent = computed(() =>
  Math.min((pollCount.value / MAX_POLLS) * 100, 95)
)

const readyStateLabel = computed(() => {
  const v = videoEl.value
  if (!v) return 'sem elemento'
  const states = ['HAVE_NOTHING','HAVE_METADATA','HAVE_CURRENT_DATA','HAVE_FUTURE_DATA','HAVE_ENOUGH_DATA']
  return `${v.readyState} (${states[v.readyState] ?? '?'})`
})

// ── Logger ───────────────────────────────────────────────────────────────────
function log(level, msg) {
  const ts = new Date().toLocaleTimeString('pt-BR', { hour12: false }) +
    '.' + String(new Date().getMilliseconds()).padStart(3, '0')
  logs.value.push({ ts, level, msg })
  nextTick(() => {
    if (consoleEl.value) consoleEl.value.scrollTop = consoleEl.value.scrollHeight
  })
}

function formatTime(s) {
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`
}

// ── Seek seguro ──────────────────────────────────────────────────────────────
// Espera o vídeo ter metadata (readyState >= 1) antes de fazer seek.
// Essencial porque o <video> pode estar montado mas ainda buffering.
async function seekTo(seconds) {
  const v = videoEl.value
  if (!v) { log('error', 'seekTo: videoEl é null!'); return }

  log('debug', `seekTo(${seconds}s) — readyState atual: ${v.readyState}`)

  if (v.readyState < 1) {
    log('debug', 'Aguardando loadedmetadata antes de fazer seek…')
    await new Promise(resolve => {
      v.addEventListener('loadedmetadata', resolve, { once: true })
    })
    log('debug', `loadedmetadata recebido — readyState: ${v.readyState}`)
  }

  const before = v.currentTime
  v.currentTime = seconds
  // Aguarda um frame para o browser aplicar o seek
  await new Promise(r => requestAnimationFrame(r))
  log('info', `seek: ${before.toFixed(1)}s → ${v.currentTime.toFixed(1)}s (alvo: ${seconds}s)`)
}

// ── Carregar episódio ─────────────────────────────────────────────────────────
// IMPORTANTE: NÃO altera phase para 'loading' ao trocar de episódio,
// pois isso destruiria o <video> via v-show e perderia o estado da reprodução.
// Só usa 'loading' na carga inicial.
async function loadEpisode(id, startAt = 0) {
  log('info', `── loadEpisode(id=${id}, startAt=${startAt}s) ──`)

  if (phase.value !== 'playing' && phase.value !== 'transitioning') {
    phase.value = 'loading'
  }

  errorMsg.value = ''

  try {
    log('info', `GET /catalog/episodes/${id}`)
    episode.value = await fetchEpisode(id)
    log('info', `Episódio: "${episode.value.title}" | intro: ${episode.value.introStartSeconds}s–${episode.value.introEndSeconds}s`)

    phase.value = 'playing'

    if (startAt > 0) {
      await seekTo(startAt)
    }

    log('info', `Chamando play()… readyState=${videoEl.value?.readyState}`)
    try {
      await videoEl.value?.play()
      log('success', 'play() OK — reprodução iniciada')
    } catch (playErr) {
      log('warn', `play() bloqueado pelo browser: ${playErr.message}`)
      log('warn', `currentTime está em ${videoEl.value?.currentTime.toFixed(1)}s — clique em Play para iniciar`)
    }

    if (startAt > 0) {
      notification.value = `Intro pulada — iniciando em ${formatTime(startAt)}`
      setTimeout(() => (notification.value = ''), 4000)
    }

  } catch (e) {
    log('error', `loadEpisode falhou: ${e.message}`)
    errorMsg.value = e.message
    phase.value = 'error'
  }
}

// ── Pular intro ───────────────────────────────────────────────────────────────
async function skipIntro() {
  if (!episode.value) return
  const target = episode.value.introEndSeconds
  log('info', `Pular intro → seek para ${target}s`)
  await seekTo(target)
}

// ── Próximo episódio ──────────────────────────────────────────────────────────
async function goNext() {
  if (!episode.value || phase.value === 'transitioning') return
  phase.value = 'transitioning'
  pollCount.value = 0
  kafkaStatus.value = 'Publicando EpisodeCompletedEvent no Kafka…'

  log('info', `── goNext: EP${episode.value.id} → próximo ──`)
  log('info', `POST /episodes/${episode.value.id}/completed`)

  try {
    const result = await completeEpisode(episode.value.id)
    log('success', `Kafka: evento publicado`)
    log('debug', `  userId: ${result.userId}`)
    log('debug', `  nextEpisodeId: ${result.nextEpisodeId}`)

    pendingNext.value = { userId: result.userId, nextEpisodeId: result.nextEpisodeId }
    kafkaStatus.value = 'Consumer processando…'
    startPolling()

  } catch (e) {
    log('error', `POST /completed falhou: ${e.message}`)
    phase.value = 'playing'
  }
}

// ── Polling ───────────────────────────────────────────────────────────────────
function startPolling() {
  const { nextEpisodeId, userId } = pendingNext.value
  log('info', `Polling GET /episodes/${nextEpisodeId}/playback-state (userId: ${userId.slice(0,8)}…)`)

  pollTimer.value = setInterval(async () => {
    pollCount.value++
    kafkaStatus.value = `Aguardando Consumer… tentativa ${pollCount.value}/${MAX_POLLS}`
    log('debug', `Poll #${pollCount.value} → GET /episodes/${nextEpisodeId}/playback-state`)

    try {
      const state = await fetchPlaybackState(nextEpisodeId, userId)
      log('debug', `  response: ${JSON.stringify(state)}`)

      if (state?.source === 'pre-computed') {
        clearInterval(pollTimer.value)
        log('success', `Consumer processou! source=pre-computed, startAtSeconds=${state.startAtSeconds}`)
        notification.value = `Consumer processou! Iniciando EP${state.episodeId} em ${formatTime(state.startAtSeconds)}`
        setTimeout(() => (notification.value = ''), 5000)
        pendingNext.value = null
        await loadEpisode(state.episodeId, state.startAtSeconds)
      } else {
        log('debug', `  source=${state?.source ?? 'null'} — ainda aguardando Consumer`)
      }

    } catch (e) {
      log('error', `Poll falhou: ${e.message}`)
    }

    if (pollCount.value >= MAX_POLLS) {
      clearInterval(pollTimer.value)
      log('warn', `Timeout de polling (${MAX_POLLS} tentativas) — carregando EP${nextEpisodeId} sem skip`)
      await loadEpisode(nextEpisodeId, 0)
      pendingNext.value = null
    }
  }, 1000)
}

// ── Eventos do vídeo ──────────────────────────────────────────────────────────
function onTimeUpdate() {
  currentTime.value = videoEl.value?.currentTime ?? 0
}

function onMetadataLoaded() {
  videoReady.value = true
  log('debug', `loadedmetadata — duration: ${videoEl.value?.duration?.toFixed(1)}s, readyState: ${videoEl.value?.readyState}`)
}

function onVideoEnded() {
  log('info', `Vídeo terminou — disparando goNext()`)
  goNext()
}

// ── Lifecycle ──────────────────────────────────────────────────────────────────
onMounted(() => {
  log('info', 'App montada — carregando EP1')
  loadEpisode(1)
})
onUnmounted(() => clearInterval(pollTimer.value))
</script>

<style scoped>
.player-shell {
  height: 100vh;
  overflow: hidden;
  background: #141414;
  display: flex;
  flex-direction: column;
  font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
}

/* ── Header ──────────────────────────────────────────────────────────────── */
.nf-header {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 14px 32px;
  background: rgba(0,0,0,0.85);
  position: relative;
  z-index: 10;
}
.nf-logo    { color: #e50914; font-size: 26px; font-weight: 900; letter-spacing: 2px; }
.nf-subtitle { color: #888; font-size: 12px; border-left: 1px solid #444; padding-left: 14px; }

/* ── Player wrap ─────────────────────────────────────────────────────────── */
.player-wrap {
  position: relative;
  background: #000;
  flex-shrink: 0;
}
.video {
  width: 100%;
  max-height: 60vh;
  display: block;
  background: #000;
}

/* ── Overlays ────────────────────────────────────────────────────────────── */
.overlay-full {
  position: absolute;
  inset: 0;
  background: rgba(0,0,0,0.85);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 16px;
  z-index: 5;
}
.overlay-info {
  position: absolute;
  bottom: 70px;
  left: 32px;
  pointer-events: none;
  z-index: 4;
}
.ep-meta  { color: #aaa; font-size: 12px; margin-bottom: 4px; text-shadow: 0 1px 4px rgba(0,0,0,0.9); }
.ep-title { font-size: 28px; font-weight: 700; text-shadow: 2px 2px 8px rgba(0,0,0,0.9); color: #fff; }

/* ── Spinner / error ─────────────────────────────────────────────────────── */
.spinner {
  width: 42px; height: 42px;
  border: 4px solid #333;
  border-top-color: #e50914;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
.loading-text { color: #aaa; font-size: 14px; }
.error-icon   { font-size: 40px; }
.error-text   { color: #e50914; font-size: 15px; }

/* ── Botão Pular Intro ───────────────────────────────────────────────────── */
.btn-skip-intro {
  position: absolute; bottom: 70px; right: 32px; z-index: 6;
  padding: 9px 22px;
  background: transparent; color: #fff;
  border: 2px solid #fff;
  font-size: 14px; font-weight: 600; cursor: pointer;
  transition: background .2s, color .2s;
}
.btn-skip-intro:hover { background: #fff; color: #000; }

/* ── Botão Próximo Episódio ──────────────────────────────────────────────── */
.btn-next {
  position: absolute; bottom: 20px; right: 32px; z-index: 6;
  padding: 10px 26px;
  background: #fff; color: #000;
  border: none; font-size: 14px; font-weight: 700; cursor: pointer;
  transition: background .2s;
}
.btn-next:hover { background: #e6e6e6; }

/* ── Finale ──────────────────────────────────────────────────────────────── */
.finale-banner {
  position: absolute; bottom: 20px; right: 32px; z-index: 6;
  padding: 10px 26px; background: #e50914; color: #fff; font-weight: 700;
}

/* ── Kafka progress ──────────────────────────────────────────────────────── */
.kafka-progress {
  position: absolute; bottom: 0; left: 0; right: 0; z-index: 6;
  padding: 6px 32px 10px; background: rgba(0,0,0,0.88);
}
.kafka-bar  { width: 100%; height: 3px; background: #333; border-radius: 2px; overflow: hidden; margin-bottom: 5px; }
.kafka-fill { height: 100%; background: #e50914; transition: width .9s ease; }
.kafka-label { color: #999; font-size: 11px; text-align: center; }

/* ── Toast ───────────────────────────────────────────────────────────────── */
.toast {
  position: fixed; bottom: 28px; left: 50%; transform: translateX(-50%);
  background: #1c1c1c; border: 1px solid #333; border-left: 4px solid #e50914;
  color: #fff; padding: 10px 22px; font-size: 13px; border-radius: 2px; z-index: 100;
  white-space: nowrap;
}

/* ── Console de debug ────────────────────────────────────────────────────── */
.console-wrap {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: #0d0d0d;
  border-top: 1px solid #1e1e1e;
  min-height: 260px;
}
.console-header {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 8px 16px;
  background: #161616;
  border-bottom: 1px solid #222;
  flex-shrink: 0;
}
.console-title  { color: #e50914; font-size: 12px; font-weight: 700; letter-spacing: 0.5px; }
.console-status { color: #555; font-size: 11px; flex: 1; font-family: monospace; }
.console-status b { color: #888; }
.console-clear  {
  margin-left: auto; background: none; border: 1px solid #333;
  color: #555; font-size: 11px; padding: 2px 8px; cursor: pointer; border-radius: 3px;
}
.console-clear:hover { color: #aaa; border-color: #555; }

.console-body {
  flex: 1;
  overflow-y: auto;
  padding: 6px 0;
  font-family: 'Courier New', Courier, monospace;
  font-size: 12px;
  line-height: 1.8;
}

.log-line {
  display: flex;
  gap: 10px;
  padding: 0 14px;
  border-left: 2px solid transparent;
}
.log-line:hover { background: #141414; }

.log-ts    { color: #3a3a3a; min-width: 90px; flex-shrink: 0; }
.log-level { min-width: 58px; flex-shrink: 0; font-weight: 700; }
.log-msg   { color: #bbb; word-break: break-all; }

.log-info    { border-left-color: #2980b9; }
.log-info    .log-level { color: #3498db; }

.log-success { border-left-color: #27ae60; }
.log-success .log-level { color: #2ecc71; }
.log-success .log-msg   { color: #ddd; }

.log-warn    { border-left-color: #d68910; }
.log-warn    .log-level { color: #f39c12; }
.log-warn    .log-msg   { color: #c8a84b; }

.log-error   { border-left-color: #c0392b; }
.log-error   .log-level { color: #e74c3c; }
.log-error   .log-msg   { color: #e57373; }

.log-debug   { border-left-color: #1e1e1e; }
.log-debug   .log-level { color: #444; }
.log-debug   .log-msg   { color: #555; }

.log-empty { padding: 12px 14px; color: #333; font-style: italic; }

/* ── Botão primário ──────────────────────────────────────────────────────── */
.btn-primary {
  padding: 9px 26px; background: #e50914; color: #fff;
  border: none; font-size: 14px; font-weight: 600; cursor: pointer;
}
.btn-primary:hover { background: #c40812; }

/* ── Transitions ─────────────────────────────────────────────────────────── */
.fade-enter-active, .fade-leave-active { transition: opacity .3s; }
.fade-enter-from, .fade-leave-to       { opacity: 0; }
.slide-up-enter-active, .slide-up-leave-active { transition: all .3s ease; }
.slide-up-enter-from, .slide-up-leave-to { opacity: 0; transform: translateX(-50%) translateY(16px); }
</style>
