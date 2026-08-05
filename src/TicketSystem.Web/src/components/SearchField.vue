<template>
  <div class="search-field">
    <label class="visually-hidden" :for="id">{{ resolvedLabel }}</label>
    <span
      v-if="loading"
      class="spinner-border spinner-border-sm search-field__icon"
      aria-hidden="true"
    ></span>
    <i v-else class="bi bi-search search-field__icon" aria-hidden="true"></i>

    <input
      :id="id"
      ref="input"
      class="form-control search-field__input"
      type="search"
      :value="modelValue"
      :placeholder="placeholder || resolvedLabel"
      :disabled="disabled"
      autocomplete="off"
      @input="$emit('update:modelValue', $event.target.value)"
    />

    <IconButton
      v-if="clearable && modelValue"
      class="search-field__clear"
      icon="bi-x-lg"
      :label="t('common.clear')"
      size="sm"
      :disabled="disabled"
      @click="clear"
    />
  </div>
</template>

<script setup>
import { computed, nextTick, ref } from 'vue'
import { useI18n } from 'vue-i18n'

import IconButton from './IconButton.vue'

const props = defineProps({
  modelValue: { type: String, default: '' },
  // Required so the visually-hidden label can be associated with the input.
  id: { type: String, required: true },
  label: { type: String, default: '' },
  placeholder: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
  loading: { type: Boolean, default: false },
  clearable: { type: Boolean, default: true },
})
const emit = defineEmits(['update:modelValue', 'clear'])

const { t } = useI18n()
const input = ref(null)
const resolvedLabel = computed(() => props.label || t('common.search'))

async function clear() {
  emit('update:modelValue', '')
  emit('clear')
  // The clear button unmounts itself the moment the value empties, so focus has
  // to be moved somewhere deliberate or it falls back to <body>.
  await nextTick()
  input.value?.focus()
}
</script>

<style scoped>
.search-field {
  position: relative;
  display: flex;
  min-width: 0;
  flex: 1;
  align-items: center;
}

.search-field__icon {
  position: absolute;
  left: 14px;
  color: var(--tv-text-subtle);
  pointer-events: none;
}

.search-field__input {
  padding-left: 42px;
  padding-right: 48px;
}

/* The native affordance sits under our own clear button and cannot be styled. */
.search-field__input::-webkit-search-cancel-button {
  display: none;
}

.search-field__clear {
  position: absolute;
  right: 4px;
}
</style>
