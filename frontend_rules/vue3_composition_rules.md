# Vue 3 Composition API Rules

## Rules for AI Agents
1. **<script setup>** is MANDATORY for all Vue components.
2. **Reactivity**: Use `ref` for primitives, `reactive` for complex objects only when necessary.
3. **Props/Emits**: Use `defineProps` and `defineEmits` clearly at the top of the script.
4. **Components**: Keep components highly cohesive and loosely coupled.
