<template>
  <!--
    Decorative: aria-hidden rather than a progressbar role. Route changes here
    are near-instant and the destination announces itself through the page title
    and heading, so a live progress announcement would be noise.
  -->
  <div v-if="visible" class="route-progress" aria-hidden="true">
    <span class="route-progress__bar" :style="{ transform: `scaleX(${value})` }"></span>
  </div>
</template>

<script setup>
import { onBeforeUnmount, ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const visible = ref(false)
const value = ref(0)

let showTimer = null
let creepTimer = null
let hideTimer = null

function clearTimers() {
  clearTimeout(showTimer)
  clearInterval(creepTimer)
  clearTimeout(hideTimer)
}

function start() {
  clearTimers()
  // A route that resolves in 20ms should not flash a bar; only navigations slow
  // enough to be noticed get one.
  showTimer = setTimeout(() => {
    visible.value = true
    value.value = 0.08
    // Creeps toward, but never reaches, the end — the remaining gap is what
    // makes the completion at the end read as "done" rather than "stopped".
    creepTimer = setInterval(() => {
      value.value = Math.min(0.9, value.value + (0.9 - value.value) * 0.18)
    }, 180)
  }, 120)
}

function finish() {
  clearTimers()
  if (!visible.value) return
  value.value = 1
  hideTimer = setTimeout(() => {
    visible.value = false
    value.value = 0
  }, 220)
}

const stopBefore = router.beforeEach(() => {
  start()
  return true
})
const stopAfter = router.afterEach(finish)
const stopError = router.onError(finish)

onBeforeUnmount(() => {
  clearTimers()
  stopBefore()
  stopAfter()
  stopError()
})
</script>

<style scoped>
.route-progress {
  position: fixed;
  top: 0;
  right: 0;
  left: 0;
  z-index: 1200;
  height: 2px;
  background: transparent;
  pointer-events: none;
}

.route-progress__bar {
  display: block;
  height: 100%;
  background: linear-gradient(90deg, var(--tv-accent), var(--tv-accent-hover));
  transform-origin: left center;
  transition: transform var(--tv-duration) var(--tv-ease);
}
</style>
