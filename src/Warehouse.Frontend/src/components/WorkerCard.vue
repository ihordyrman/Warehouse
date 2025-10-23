<template>
    <div class="card">
        <div class="flex justify-between items-start mb-4">
            <div>
                <h3 class="text-lg font-semibold text-gray-900">{{ worker.symbol }}</h3>
                <span class="text-sm text-gray-500">{{ marketTypeDisplay }}</span>
            </div>
            <button
                    @click="toggleStatus"
                    :class="statusButtonClass"
                    class="px-3 py-1 rounded-md text-sm font-medium transition-colors"
            >
                {{ worker.enabled ? 'Running' : 'Stopped' }}
            </button>
        </div>

        <div class="space-y-3">
            <div class="grid grid-cols-2 gap-4 text-sm">
                <div>
                    <span class="text-gray-500">Strategy:</span>
                    <p class="font-medium text-gray-900">{{ worker.strategy || 'None' }}</p>
                </div>
                <div>
                    <span class="text-gray-500">Interval:</span>
                    <p class="font-medium text-gray-900">{{ worker.interval || '1m' }}</p>
                </div>
            </div>

            <div class="pt-3 border-t border-gray-200">
                <div class="flex items-center justify-between text-sm">
                    <span class="text-gray-500">Last Updated:</span>
                    <span class="text-gray-700">{{ formatDateTime(worker.updatedAt) }}</span>
                </div>

                <div v-if="worker.enabled" class="mt-3">
                    <div class="flex items-center text-sm text-green-600">
                        <svg class="w-4 h-4 mr-1 animate-pulse" fill="currentColor" viewBox="0 0 20 20">
                            <circle cx="10" cy="10" r="3" />
                        </svg>
                        Processing market data...
                    </div>
                </div>
            </div>

            <div v-if="worker.pipelineSteps && worker.pipelineSteps.length > 0" class="pt-3 border-t border-gray-200">
                <div class="text-sm text-gray-600 mb-2">Pipeline Steps:</div>
                <div class="space-y-1">
                    <div
                            v-for="step in worker.pipelineSteps"
                            :key="step.id"
                            class="flex items-center text-sm"
                    >
            <span class="w-6 h-6 rounded-full bg-primary-100 text-primary-600 flex items-center justify-center text-xs mr-2">
              {{ step.order }}
            </span>
                        <span class="text-gray-700">{{ step.name }}</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    name: 'WorkerCard',
    props: {
        worker: {
            type: Object,
            required: true,
            default: () => ({
                id: 0,
                symbol: '',
                type: 0,
                enabled: false,
                strategy: '',
                interval: '',
                pipelineSteps: [],
                updatedAt: null
            })
        }
    },
    computed: {
        marketTypeDisplay() {
            const marketTypes = {
                0: 'OKX',
                1: 'Binance'
            }
            return marketTypes[this.worker.type] || 'Unknown'
        },
        statusButtonClass() {
            return this.worker.enabled
                    ? 'bg-green-100 text-green-800 hover:bg-green-200'
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
        }
    },
    methods: {
        toggleStatus() {
            this.$emit('toggle', this.worker.id, !this.worker.enabled)
        },
        formatDateTime(date) {
            if (!date) return 'Never'
            const options = {
                month: 'short',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            }
            return new Date(date).toLocaleString('en-US', options)
        }
    }
}
</script>
